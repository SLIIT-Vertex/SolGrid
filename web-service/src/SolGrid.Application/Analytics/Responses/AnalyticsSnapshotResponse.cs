/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AnalyticsSnapshotResponse.cs
 * Description: Returns node, reservation, and business-rule analytics calculated by the service.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Analytics.Responses;

public sealed class AnalyticsSnapshotResponse
{
    public DateTimeOffset GeneratedAtUtc { get; init; }

    public NetworkAnalyticsResponse Network { get; init; } = new();

    public ReservationAnalyticsResponse Reservations { get; init; } = new();

    public IReadOnlyList<NodeAnalyticsResponse> Nodes { get; init; } = [];

    public IReadOnlyList<BusinessRoutineResponse> Routines { get; init; } = [];

    public AccountAnalyticsResponse? Accounts { get; init; }
}

public sealed class NetworkAnalyticsResponse
{
    public int NodeCount { get; init; }

    public int ActiveNodes { get; init; }

    public int InactiveNodes { get; init; }

    public int NodesWithOpenSlots { get; init; }

    public decimal TotalCapacityKw { get; init; }

    public decimal TotalBatteryKwh { get; init; }

    public decimal CommittedBatteryKwh { get; init; }

    public int TotalSlots { get; init; }

    public int AvailableSlots { get; init; }

    public int ReservedSlots { get; init; }

    public int OccupiedSlots { get; init; }

    public int OutOfServiceSlots { get; init; }
}

public sealed class ReservationAnalyticsResponse
{
    public int Total { get; init; }

    public int Pending { get; init; }

    public int Approved { get; init; }

    public int Rejected { get; init; }

    public int Cancelled { get; init; }

    public int Completed { get; init; }

    public int ApprovedFuture { get; init; }

    public int Current { get; init; }

    public int History { get; init; }

    public int InsideSevenDayWindow { get; init; }

    public int BeyondSevenDayWindow { get; init; }

    public int Changeable { get; init; }

    public int LockedByNotice { get; init; }

    public int PastStillOpen { get; init; }

    public int ApprovedAwaitingQr { get; init; }

    public int QrLive { get; init; }

    public int QrExpired { get; init; }

    public int QrVerified { get; init; }
}

public sealed class NodeAnalyticsResponse
{
    public string Id { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public decimal CapacityKw { get; init; }

    public StationStatus Status { get; init; }

    public int ScheduleDayCount { get; init; }

    public int TotalSlots { get; init; }

    public int AvailableSlots { get; init; }

    public int ReservedSlots { get; init; }

    public int OccupiedSlots { get; init; }

    public int OutOfServiceSlots { get; init; }

    public decimal TotalBatteryKwh { get; init; }

    public decimal CommittedBatteryKwh { get; init; }

    public int PendingReservations { get; init; }

    public int ApprovedReservations { get; init; }

    public int RejectedReservations { get; init; }

    public int CancelledReservations { get; init; }

    public int CompletedReservations { get; init; }

    public int ActiveReservations { get; init; }

    public bool DeactivationBlocked { get; init; }

    public int SlotStateDrift { get; init; }

    public double UtilizationPercent { get; init; }
}

public sealed class BusinessRoutineResponse
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Rule { get; init; } = string.Empty;

    public BusinessRoutineOutcome Outcome { get; init; }

    public int MeasuredCount { get; init; }

    public string Detail { get; init; } = string.Empty;
}

public sealed class AccountAnalyticsResponse
{
    public int WebUsers { get; init; }

    public int ActiveWebUsers { get; init; }

    public int BackofficeUsers { get; init; }

    public int GridOperatorUsers { get; init; }

    public int Prosumers { get; init; }

    public int PendingProsumers { get; init; }

    public int ActiveProsumers { get; init; }

    public int DeactivationRequested { get; init; }

    public int DeactivatedProsumers { get; init; }
}
