/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BookingSlotQuery.cs
 * Description: Defines paged booking slot filters independent of database query types.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Interfaces;

public sealed class BookingSlotQuery
{
    public string? StationId { get; init; }

    public SlotStatus? Status { get; init; }

    // Optional bounds select slots overlapping [From, To); each bound may be omitted.
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
