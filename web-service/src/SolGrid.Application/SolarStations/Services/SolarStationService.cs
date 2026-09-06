/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationService.cs
 * Description: Handles microgrid solar station management business use cases.
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
using SolGrid.Domain.ValueObjects;

namespace SolGrid.Application.SolarStations.Services;

public sealed class SolarStationService : ISolarStationService
{
    private static readonly CreateSolarStationRequestValidator CreateValidator = new();
    private static readonly UpdateSolarStationRequestValidator UpdateValidator = new();
    private static readonly SolarStationQueryValidator QueryValidator = new();
    private static readonly UpdateStationScheduleRequestValidator ScheduleValidator = new();

    private readonly ISolarStationRepository stationRepository;
    private readonly IBookingSlotRepository bookingSlotRepository;
    private readonly IStationReservationLookup reservationLookup;
    private readonly ICurrentUserContext currentUserContext;
    private readonly TimeProvider timeProvider;

    public SolarStationService(
        ISolarStationRepository stationRepository,
        IBookingSlotRepository bookingSlotRepository,
        IStationReservationLookup reservationLookup,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        // Capture station management dependencies without coupling to infrastructure implementations.
        this.stationRepository = stationRepository;
        this.bookingSlotRepository = bookingSlotRepository;
        this.reservationLookup = reservationLookup;
        this.currentUserContext = currentUserContext;
        this.timeProvider = timeProvider;
    }

    public async Task<SolarStationResponse> CreateStationAsync(
        CreateSolarStationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate station details and code uniqueness before creating the node.
        EnsureStationAdministrator();
        EnsureValid(CreateValidator.Validate(request));

        if (await stationRepository.ExistsByCodeAsync(request.Code, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException("Station code is already assigned to another station.");
        }

        var createdAt = timeProvider.GetUtcNow();
        var stationId = Guid.NewGuid().ToString("N");
        var station = SolarStation.Create(
            stationId,
            request.Code,
            request.Name,
            request.AddressLine,
            GeoCoordinates.Create(request.Location!.Latitude, request.Location.Longitude),
            request.CapacityKw,
            request.Slots.Select(slot => CreateSlot(stationId, slot, createdAt)),
            request.Schedule.Select(ToOperatingWindow),
            createdAt);

        await stationRepository.AddAsync(station, cancellationToken).ConfigureAwait(false);
        foreach (var slot in station.Slots)
        {
            await bookingSlotRepository.AddAsync(slot, cancellationToken).ConfigureAwait(false);
        }

        return SolarStationResponseMapper.ToResponse(station);
    }

    public async Task<SolarStationResponse> UpdateStationAsync(
        string id,
        UpdateSolarStationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate the complete edit before applying domain changes.
        EnsureStationAdministrator();
        ValidateId(id);
        EnsureValid(UpdateValidator.Validate(request));

        var station = await GetRequiredStationAsync(id, cancellationToken).ConfigureAwait(false);

        if (await stationRepository.ExistsByCodeAsync(request.Code, station.Id, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException("Station code is already assigned to another station.");
        }

        station.UpdateDetails(
            request.Code,
            request.Name,
            request.AddressLine,
            GeoCoordinates.Create(request.Location!.Latitude, request.Location.Longitude),
            request.CapacityKw,
            timeProvider.GetUtcNow());

        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(station);
    }

    public async Task<SolarStationResponse> GetStationByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Return a station response or report the shared not-found application error.
        EnsureStationReader();
        ValidateId(id);
        var station = await GetRequiredStationAsync(id, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(station);
    }

    public async Task<PagedResult<SolarStationResponse>> GetStationsAsync(
        SolarStationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validate filters and return the requested page of stations.
        EnsureStationReader();
        EnsureValid(QueryValidator.Validate(query));

        var stations = await stationRepository.GetPagedAsync(query, cancellationToken).ConfigureAwait(false);
        return new PagedResult<SolarStationResponse>
        {
            Items = stations.Items.Select(station => SolarStationResponseMapper.ToResponse(station)).ToArray(),
            TotalCount = stations.TotalCount,
            PageNumber = stations.PageNumber,
            PageSize = stations.PageSize
        };
    }

    public async Task<IReadOnlyList<SolarStationResponse>> GetNearbyStationsAsync(
        NearbyStationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Preserve the existing discovery contract for later client implementation.
        EnsureStationReader();
        EnsureValid(SolarStationValidationRules.ValidateNearbyQuery(query));

        var origin = GeoCoordinates.Create(query.Latitude, query.Longitude);
        var stations = await stationRepository.GetNearbyAsync(query, cancellationToken).ConfigureAwait(false);
        return stations.Select(station => SolarStationResponseMapper.ToResponse(station, origin)).ToArray();
    }

    public async Task<SolarStationResponse> ReplaceScheduleAsync(
        string id,
        UpdateStationScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate weekly windows before replacing the schedule through the domain entity.
        EnsureStationAdministrator();
        ValidateId(id);
        EnsureValid(ScheduleValidator.Validate(request));

        var station = await GetRequiredStationAsync(id, cancellationToken).ConfigureAwait(false);
        station.ReplaceSchedule(request.Schedule.Select(ToOperatingWindow), timeProvider.GetUtcNow());
        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
        return SolarStationResponseMapper.ToResponse(station);
    }

    public async Task ActivateStationAsync(string id, CancellationToken cancellationToken = default)
    {
        // Activate through the domain entity after server-side authorization.
        EnsureStationAdministrator();
        ValidateId(id);

        var station = await GetRequiredStationAsync(id, cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => station.Activate(timeProvider.GetUtcNow()));
        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateStationAsync(string id, CancellationToken cancellationToken = default)
    {
        // Block deactivation when IStationReservationLookup reports any active reservation.
        EnsureStationAdministrator();
        ValidateId(id);

        var station = await GetRequiredStationAsync(id, cancellationToken).ConfigureAwait(false);
        var activeReservationCount = await reservationLookup
            .CountActiveReservationsAsync(station.Id, cancellationToken)
            .ConfigureAwait(false);

        if (activeReservationCount > 0)
        {
            throw new ConflictException("A station with active reservations cannot be deactivated.");
        }

        ApplyLifecycleTransition(() => station.Deactivate(timeProvider.GetUtcNow()));
        await stationRepository.UpdateAsync(station, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SolarStation> GetRequiredStationAsync(string id, CancellationToken cancellationToken)
    {
        // Load a station or return a client-safe not-found error.
        var station = await stationRepository.GetByIdAsync(id.Trim(), cancellationToken).ConfigureAwait(false);
        return station ?? throw new NotFoundException("Station", id.Trim());
    }

    private void EnsureStationAdministrator()
    {
        // Restrict administrative station writes to Backoffice users.
        if (currentUserContext.Role != UserRole.Backoffice)
        {
            throw new ForbiddenException("Backoffice authorization is required for station administration.");
        }
    }

    private void EnsureStationReader()
    {
        // Restrict station reads to operational web roles.
        if (currentUserContext.Role is not (UserRole.Backoffice or UserRole.GridOperator))
        {
            throw new ForbiddenException("Backoffice or GridOperator authorization is required to view stations.");
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
        // Throw one validation exception containing every collected nearby-query error.
        var errorList = errors.ToArray();
        if (errorList.Length > 0)
        {
            throw new ValidationException(errorList);
        }
    }

    private static void ValidateId(string id)
    {
        // Validate that route ids contain meaningful text.
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException(["Station id is required."]);
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

    private static EnergyBookingSlot CreateSlot(
        string stationId,
        CreateBookingSlotRequest request,
        DateTimeOffset createdAt)
    {
        // Create an owned slot from station-create payload values.
        return EnergyBookingSlot.Create(
            Guid.NewGuid().ToString("N"),
            stationId,
            request.SlotNumber,
            request.BatteryCapacityKwh,
            request.StartTime,
            request.EndTime,
            createdAt);
    }

    private static OperatingWindow ToOperatingWindow(OperatingWindowRequest request)
    {
        // Convert a validated weekly window request into a domain value object.
        return OperatingWindow.Create(request.Day, request.OpensAt, request.ClosesAt);
    }
}
