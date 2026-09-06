/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BookingSlotService.cs
 * Description: Handles energy booking slot management business use cases.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;
using SolGrid.Application.SolarStations.Validation;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Services;

public sealed class BookingSlotService : IBookingSlotService
{
    private static readonly CreateBookingSlotRequestValidator CreateValidator = new();
    private static readonly UpdateBookingSlotRequestValidator UpdateValidator = new();
    private static readonly BookingSlotQueryValidator QueryValidator = new();

    private readonly ISolarStationRepository stationRepository;
    private readonly IBookingSlotRepository bookingSlotRepository;
    private readonly IStationReservationLookup reservationLookup;
    private readonly ICurrentUserContext currentUserContext;
    private readonly TimeProvider timeProvider;

    public BookingSlotService(
        ISolarStationRepository stationRepository,
        IBookingSlotRepository bookingSlotRepository,
        IStationReservationLookup reservationLookup,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        // Capture slot management dependencies without coupling to infrastructure implementations.
        this.stationRepository = stationRepository;
        this.bookingSlotRepository = bookingSlotRepository;
        this.reservationLookup = reservationLookup;
        this.currentUserContext = currentUserContext;
        this.timeProvider = timeProvider;
    }

    public async Task<EnergyBookingSlotResponse> CreateBookingSlotAsync(
        string stationId,
        CreateBookingSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate station eligibility, schedule coverage, and uniqueness before persisting a slot.
        EnsureSlotAdministrator();
        ValidateId(stationId, "Station id is required.");
        EnsureValid(CreateValidator.Validate(request));

        var station = await GetRequiredStationAsync(stationId, cancellationToken).ConfigureAwait(false);
        EnsureStationIsActive(station);
        EnsureStationHasCapacityForAnotherSlot(station);
        EnsureValid(SolarStationValidationRules.ValidateSlotAgainstSchedule(
            station.Schedule, request.StartTime, request.EndTime));

        if (await bookingSlotRepository
                .ExistsBySlotNumberAsync(station.Id, request.SlotNumber, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException($"Slot number {request.SlotNumber} is already used by this station.");
        }

        var createdAt = timeProvider.GetUtcNow();
        var slot = EnergyBookingSlot.Create(
            Guid.NewGuid().ToString("N"),
            station.Id,
            request.SlotNumber,
            request.BatteryCapacityKwh,
            request.StartTime,
            request.EndTime,
            createdAt);

        ApplyLifecycleTransition(() => station.AddSlot(slot, createdAt));
        await PersistNewSlotAsync(station, slot, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(slot);
    }

    public async Task<EnergyBookingSlotResponse> UpdateBookingSlotAsync(
        string id,
        UpdateBookingSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        // Protect immutable identity while replacing capacity and the booking interval.
        EnsureSlotAdministrator();
        ValidateId(id, "Booking slot id is required.");
        EnsureValid(UpdateValidator.Validate(request));

        var slot = await GetRequiredSlotAsync(id, cancellationToken).ConfigureAwait(false);
        var station = await GetRequiredStationAsync(slot.StationId, cancellationToken).ConfigureAwait(false);
        EnsureStationOwnsSlot(station, slot);
        await EnsureSlotHasNoActiveReservationAsync(slot.Id, cancellationToken).ConfigureAwait(false);
        EnsureValid(SolarStationValidationRules.ValidateSlotAgainstSchedule(
            station.Schedule, request.StartTime, request.EndTime));

        ApplyLifecycleTransition(() => slot.UpdateDetails(
            request.BatteryCapacityKwh, request.StartTime, request.EndTime, timeProvider.GetUtcNow()));
        await PersistUpdatedSlotAsync(station, slot, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(slot);
    }

    public async Task<EnergyBookingSlotResponse> GetBookingSlotByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Return a slot response or report the shared not-found application error.
        EnsureAuthenticatedSlotReader();
        ValidateId(id, "Booking slot id is required.");
        var slot = await GetRequiredSlotAsync(id, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(slot);
    }

    public async Task<PagedResult<EnergyBookingSlotResponse>> GetBookingSlotsAsync(
        BookingSlotQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validate filters, confirm the owning station exists, and return the requested page.
        EnsureAuthenticatedSlotReader();
        EnsureValid(QueryValidator.Validate(query));
        ValidateId(query.StationId, "Station id is required.");

        await GetRequiredStationAsync(query.StationId!, cancellationToken).ConfigureAwait(false);
        var slots = await bookingSlotRepository.GetPagedAsync(query, cancellationToken).ConfigureAwait(false);
        return new PagedResult<EnergyBookingSlotResponse>
        {
            Items = slots.Items.Select(SolarStationResponseMapper.ToResponse).ToArray(),
            TotalCount = slots.TotalCount,
            PageNumber = slots.PageNumber,
            PageSize = slots.PageSize
        };
    }

    public async Task ActivateBookingSlotAsync(string id, CancellationToken cancellationToken = default)
    {
        // Return an out-of-service slot to the bookable pool through the domain transition.
        EnsureSlotAdministrator();
        ValidateId(id, "Booking slot id is required.");

        var slot = await GetRequiredSlotAsync(id, cancellationToken).ConfigureAwait(false);
        var station = await GetRequiredStationAsync(slot.StationId, cancellationToken).ConfigureAwait(false);
        EnsureStationOwnsSlot(station, slot);
        ApplyLifecycleTransition(() => slot.ReturnToService(timeProvider.GetUtcNow()));
        await PersistUpdatedSlotAsync(station, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateBookingSlotAsync(string id, CancellationToken cancellationToken = default)
    {
        // Withdraw the slot from bookings without deleting history referenced by reservations.
        EnsureSlotAdministrator();
        ValidateId(id, "Booking slot id is required.");

        var slot = await GetRequiredSlotAsync(id, cancellationToken).ConfigureAwait(false);
        var station = await GetRequiredStationAsync(slot.StationId, cancellationToken).ConfigureAwait(false);
        EnsureStationOwnsSlot(station, slot);
        await EnsureSlotHasNoActiveReservationAsync(slot.Id, cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => slot.TakeOutOfService(timeProvider.GetUtcNow()));
        await PersistUpdatedSlotAsync(station, slot, cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistNewSlotAsync(
        SolarStation station,
        EnergyBookingSlot slot,
        CancellationToken cancellationToken)
    {
        // Persist the slot record first, then keep station membership counts aligned.
        await bookingSlotRepository.AddAsync(slot, cancellationToken).ConfigureAwait(false);
        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistUpdatedSlotAsync(
        SolarStation station,
        EnergyBookingSlot slot,
        CancellationToken cancellationToken)
    {
        // Replace the nested station copy so derived slot counts stay consistent.
        ApplyLifecycleTransition(() => ReplaceStationSlot(station, slot, timeProvider.GetUtcNow()));
        await bookingSlotRepository.UpdateAsync(slot, cancellationToken).ConfigureAwait(false);
        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
    }

    private static void ReplaceStationSlot(SolarStation station, EnergyBookingSlot slot, DateTimeOffset updatedAt)
    {
        // Rebuild station membership from the authoritative slot record.
        if (station.FindSlot(slot.Id) is not null)
        {
            station.RemoveSlot(slot.Id, updatedAt);
        }

        station.AddSlot(slot, updatedAt);
    }

    private async Task<SolarStation> GetRequiredStationAsync(string id, CancellationToken cancellationToken)
    {
        // Load a station or return a client-safe not-found error.
        var station = await stationRepository.GetByIdAsync(id.Trim(), cancellationToken).ConfigureAwait(false);
        return station ?? throw new NotFoundException("Station", id.Trim());
    }

    private async Task<EnergyBookingSlot> GetRequiredSlotAsync(string id, CancellationToken cancellationToken)
    {
        // Load a slot or return a client-safe not-found error.
        var slot = await bookingSlotRepository.GetByIdAsync(id.Trim(), cancellationToken).ConfigureAwait(false);
        return slot ?? throw new NotFoundException("BookingSlot", id.Trim());
    }

    private async Task EnsureSlotHasNoActiveReservationAsync(string slotId, CancellationToken cancellationToken)
    {
        // Block destructive edits while the reservation module still holds a live booking.
        var hasActiveReservation = await reservationLookup
            .HasActiveReservationForSlotAsync(slotId, cancellationToken)
            .ConfigureAwait(false);

        if (hasActiveReservation)
        {
            throw new ConflictException("A slot with active reservations cannot be changed.");
        }
    }

    private void EnsureSlotAdministrator()
    {
        // Restrict administrative slot writes to Backoffice users.
        if (currentUserContext.Role != UserRole.Backoffice)
        {
            throw new ForbiddenException("Backoffice authorization is required for booking slot administration.");
        }
    }

    private void EnsureAuthenticatedSlotReader()
    {
        // Allow Android booking screens and operational clients to read live slot availability.
        if (!currentUserContext.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required to view booking slots.");
        }
    }

    private static void EnsureStationIsActive(SolarStation station)
    {
        // New slots can only be added to stations that currently accept bookings.
        if (!station.IsActive)
        {
            throw new ConflictException("Station is not active for booking slot management.");
        }
    }

    private static void EnsureStationHasCapacityForAnotherSlot(SolarStation station)
    {
        // Enforce the existing per-station slot-list size before adding hardware.
        if (station.TotalSlotCount >= SolarStationValidationRules.MaximumSlotsPerStation)
        {
            throw new ValidationException(
                [$"A station must not declare more than {SolarStationValidationRules.MaximumSlotsPerStation} battery storage slots."]);
        }
    }

    private static void EnsureStationOwnsSlot(SolarStation station, EnergyBookingSlot slot)
    {
        // Reject mutations that would reassign a slot onto a different station.
        if (!string.Equals(slot.StationId, station.Id, StringComparison.Ordinal))
        {
            throw new ValidationException(["Booking slot does not belong to the requested station."]);
        }
    }

    private static void EnsureValid(ValidationResult result)
    {
        // Throw one validation exception containing every collected request error.
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private static void EnsureValid(IEnumerable<string> errors)
    {
        // Throw one validation exception containing every collected schedule error.
        var errorList = errors.ToArray();
        if (errorList.Length > 0)
        {
            throw new ValidationException(errorList);
        }
    }

    private static void ValidateId(string? id, string message)
    {
        // Validate that route ids contain meaningful text.
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException([message]);
        }
    }

    private static void ApplyLifecycleTransition(Action transition)
    {
        // Translate domain lifecycle violations into API conflict behavior.
        try
        {
            transition();
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}
