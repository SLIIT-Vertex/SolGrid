/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationReferenceReadServices.cs
 * Description: Defines small cross-component read contracts required by reservation use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Prosumers.Responses;

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationProsumerReadService
{
    Task<ReservationProsumerSnapshot?> GetByIdAsync(string prosumerId, CancellationToken cancellationToken = default);
}

public interface IReservationStationReadService
{
    Task<ReservationStationSnapshot?> GetByIdAsync(string stationId, CancellationToken cancellationToken = default);
}

public interface IReservationBookingSlotReadService
{
    Task<ReservationBookingSlotSnapshot?> GetByIdAsync(string bookingSlotId, CancellationToken cancellationToken = default);

    // Hold the slot once its reservation is approved, so it stops appearing bookable to others.
    Task ReserveAsync(string bookingSlotId, CancellationToken cancellationToken = default);

    // Mark the slot physically in use once the prosumer's arrival QR is verified.
    Task OccupyAsync(string bookingSlotId, CancellationToken cancellationToken = default);

    // Return the slot to the bookable pool once its reservation ends (rejected, cancelled, or completed).
    Task ReleaseAsync(string bookingSlotId, CancellationToken cancellationToken = default);
}

public sealed class ReservationProsumerSnapshot
{
    public string Id { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string Nic { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public ProsumerResponse? Details { get; init; }
}

public sealed class ReservationStationSnapshot
{
    public string Id { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ReservationBookingSlotSnapshot
{
    public string Id { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsAvailable { get; init; }

    // Reserved window boundaries, used to gate energy-transfer finalization to the booked time.
    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }
}
