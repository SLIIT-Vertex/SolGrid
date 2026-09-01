/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationService.cs
 * Description: Handles energy reservation business use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;
using SolGrid.Domain.Entities;

namespace SolGrid.Application.Reservations.Services;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository reservationRepository;
    private readonly IReservationProsumerReadService prosumerReadService;
    private readonly IReservationStationReadService stationReadService;
    private readonly IReservationBookingSlotReadService bookingSlotReadService;
    private readonly TimeProvider timeProvider;

    public ReservationService(
        IReservationRepository reservationRepository,
        IReservationProsumerReadService prosumerReadService,
        IReservationStationReadService stationReadService,
        IReservationBookingSlotReadService bookingSlotReadService,
        TimeProvider timeProvider)
    {
        // Capture reservation workflow dependencies.
        this.reservationRepository = reservationRepository;
        this.prosumerReadService = prosumerReadService;
        this.stationReadService = stationReadService;
        this.bookingSlotReadService = bookingSlotReadService;
        this.timeProvider = timeProvider;
    }

    public async Task<ReservationResponse> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate references, enforce slot availability rules, and persist a pending reservation.
        ValidateCreateRequest(request);

        var prosumerId = NormalizeIdentifier(request.ProsumerId);
        var stationId = NormalizeIdentifier(request.StationId);
        var bookingSlotId = NormalizeIdentifier(request.BookingSlotId);
        var scheduledAtUtc = ReservationTimeRules.NormalizeToUtc(request.ScheduledAt);
        var nowUtc = timeProvider.GetUtcNow();

        ValidateSchedule(scheduledAtUtc, nowUtc);

        await EnsureProsumerCanReserveAsync(prosumerId, cancellationToken).ConfigureAwait(false);
        await EnsureStationCanBeReservedAsync(stationId, cancellationToken).ConfigureAwait(false);
        await EnsureBookingSlotCanBeReservedAsync(bookingSlotId, stationId, cancellationToken).ConfigureAwait(false);

        if (await reservationRepository
                .HasActiveReservationForBookingSlotAsync(bookingSlotId, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException("Booking slot already has an active reservation.");
        }

        var reservation = EnergyReservation.Create(
            Guid.NewGuid().ToString("N"),
            prosumerId,
            stationId,
            bookingSlotId,
            scheduledAtUtc,
            nowUtc);

        await reservationRepository.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    private static void ValidateCreateRequest(CreateReservationRequest request)
    {
        // Validate required reservation create request fields.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ProsumerId))
        {
            errors.Add("Prosumer id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.StationId))
        {
            errors.Add("Station id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.BookingSlotId))
        {
            errors.Add("Booking slot id is required.");
        }

        if (request.ScheduledAt == default)
        {
            errors.Add("Scheduled date and time is required.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateSchedule(DateTimeOffset scheduledAtUtc, DateTimeOffset nowUtc)
    {
        // Validate assignment scheduling rules using centralized time-window helpers.
        var errors = new List<string>();

        if (ReservationTimeRules.IsScheduledInPast(scheduledAtUtc, nowUtc))
        {
            errors.Add("Reservation cannot be scheduled in the past.");
        }

        if (ReservationTimeRules.IsBeyondMaximumReservationWindow(scheduledAtUtc, nowUtc))
        {
            errors.Add($"Reservation must be scheduled within {ReservationTimeRules.MaximumAdvanceReservationDays} days.");
        }

        ThrowIfInvalid(errors);
    }

    private async Task EnsureProsumerCanReserveAsync(string prosumerId, CancellationToken cancellationToken)
    {
        // Verify the referenced prosumer exists and can make reservations.
        var prosumer = await prosumerReadService.GetByIdAsync(prosumerId, cancellationToken).ConfigureAwait(false);

        if (prosumer is null)
        {
            throw new NotFoundException("Prosumer", prosumerId);
        }

        if (!prosumer.IsActive)
        {
            throw new ConflictException("Prosumer account is not active for reservations.");
        }
    }

    private async Task EnsureStationCanBeReservedAsync(string stationId, CancellationToken cancellationToken)
    {
        // Verify the referenced station exists and is active.
        var station = await stationReadService.GetByIdAsync(stationId, cancellationToken).ConfigureAwait(false);

        if (station is null)
        {
            throw new NotFoundException("Station", stationId);
        }

        if (!station.IsActive)
        {
            throw new ConflictException("Station is not active for reservations.");
        }
    }

    private async Task EnsureBookingSlotCanBeReservedAsync(
        string bookingSlotId,
        string stationId,
        CancellationToken cancellationToken)
    {
        // Verify the booking slot exists, belongs to the station, and is available.
        var bookingSlot = await bookingSlotReadService.GetByIdAsync(bookingSlotId, cancellationToken).ConfigureAwait(false);

        if (bookingSlot is null)
        {
            throw new NotFoundException("BookingSlot", bookingSlotId);
        }

        if (!string.Equals(bookingSlot.StationId, stationId, StringComparison.Ordinal))
        {
            throw new ValidationException(["Booking slot does not belong to the requested station."]);
        }

        if (!bookingSlot.IsActive || !bookingSlot.IsAvailable)
        {
            throw new ConflictException("Booking slot is not available for reservations.");
        }
    }

    private static void ThrowIfInvalid(IEnumerable<string> errors)
    {
        // Throw one validation exception containing all collected validation messages.
        var errorList = errors.ToArray();
        if (errorList.Length > 0)
        {
            throw new ValidationException(errorList);
        }
    }

    private static string NormalizeIdentifier(string identifier)
    {
        // Trim identifier values before cross-component lookups and persistence.
        return identifier.Trim();
    }
}
