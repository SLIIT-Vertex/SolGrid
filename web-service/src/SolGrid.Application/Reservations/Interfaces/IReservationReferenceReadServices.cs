/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationReferenceReadServices.cs
 * Description: Defines small cross-component read contracts required by reservation use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

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
}

public sealed class ReservationProsumerSnapshot
{
    public string Id { get; init; } = string.Empty;

    public bool IsActive { get; init; }
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
}
