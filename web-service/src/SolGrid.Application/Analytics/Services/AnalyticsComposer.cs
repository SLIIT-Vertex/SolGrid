/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AnalyticsComposer.cs
 * Description: Evaluates node, reservation, and business-rule analytics from loaded records.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Application.Analytics.Responses;
using SolGrid.Application.Reservations.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Analytics.Services;

public static class AnalyticsComposer
{
    public static AnalyticsSnapshotResponse Compose(
        IReadOnlyList<SolarStation> stations,
        IReadOnlyList<EnergyReservation> reservations,
        IReadOnlyList<User>? users,
        IReadOnlyList<Prosumer>? prosumers,
        DateTimeOffset nowUtc,
        bool includeAccounts)
    {
        // Build one analytics snapshot from the records the service has already loaded.
        var now = nowUtc.ToUniversalTime();
        var windowEnd = now.AddDays(ReservationTimeRules.MaximumAdvanceReservationDays);
        var notice = TimeSpan.FromHours(ReservationTimeRules.MinimumChangeNoticeHours);
        var slots = stations.SelectMany(station => station.Slots).ToArray();
        var slotById = slots.ToDictionary(slot => slot.Id);
        var stationIds = stations.Select(station => station.Id).ToHashSet();
        var activeBySlot = reservations
            .Where(reservation => reservation.IsActive)
            .GroupBy(reservation => reservation.BookingSlotId)
            .ToDictionary(group => group.Key, group => group.Count());

        var nodes = stations
            .Select(station => ComposeNode(station, reservations, activeBySlot))
            .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var reservationsAnalytics = ComposeReservations(reservations, now, windowEnd, notice);
        var routines = ComposeRoutines(
            stations,
            slots,
            reservations,
            users,
            prosumers,
            slotById,
            stationIds,
            activeBySlot,
            now,
            windowEnd,
            notice,
            includeAccounts);

        return new AnalyticsSnapshotResponse
        {
            GeneratedAtUtc = now,
            Network = ComposeNetwork(nodes),
            Reservations = reservationsAnalytics,
            Nodes = nodes,
            Routines = routines,
            Accounts = includeAccounts ? ComposeAccounts(users ?? [], prosumers ?? []) : null
        };
    }

    private static NetworkAnalyticsResponse ComposeNetwork(IReadOnlyList<NodeAnalyticsResponse> nodes)
    {
        // Sum node figures so the page does not recalculate network totals.
        return new NetworkAnalyticsResponse
        {
            NodeCount = nodes.Count,
            ActiveNodes = nodes.Count(node => node.Status == StationStatus.Active),
            InactiveNodes = nodes.Count(node => node.Status == StationStatus.Inactive),
            NodesWithOpenSlots = nodes.Count(node => node.AvailableSlots > 0),
            TotalCapacityKw = nodes.Sum(node => node.CapacityKw),
            TotalBatteryKwh = nodes.Sum(node => node.TotalBatteryKwh),
            CommittedBatteryKwh = nodes.Sum(node => node.CommittedBatteryKwh),
            TotalSlots = nodes.Sum(node => node.TotalSlots),
            AvailableSlots = nodes.Sum(node => node.AvailableSlots),
            ReservedSlots = nodes.Sum(node => node.ReservedSlots),
            OccupiedSlots = nodes.Sum(node => node.OccupiedSlots),
            OutOfServiceSlots = nodes.Sum(node => node.OutOfServiceSlots)
        };
    }

    private static ReservationAnalyticsResponse ComposeReservations(
        IReadOnlyList<EnergyReservation> reservations,
        DateTimeOffset now,
        DateTimeOffset windowEnd,
        TimeSpan notice)
    {
        // Count reservation states using the same windows the booking service enforces.
        var active = reservations.Where(reservation => reservation.IsActive).ToArray();
        var futureActive = active.Where(reservation => reservation.ScheduledAt.ToUniversalTime() >= now).ToArray();

        return new ReservationAnalyticsResponse
        {
            Total = reservations.Count,
            Pending = reservations.Count(reservation => reservation.Status == ReservationStatus.Pending),
            Approved = reservations.Count(reservation => reservation.Status == ReservationStatus.Approved),
            Rejected = reservations.Count(reservation => reservation.Status == ReservationStatus.Rejected),
            Cancelled = reservations.Count(reservation => reservation.Status == ReservationStatus.Cancelled),
            Completed = reservations.Count(reservation => reservation.Status == ReservationStatus.Completed),
            ApprovedFuture = reservations.Count(reservation =>
                reservation.Status == ReservationStatus.Approved && reservation.ScheduledAt.ToUniversalTime() >= now),
            Current = futureActive.Length,
            History = reservations.Count(reservation =>
                reservation.Status is ReservationStatus.Rejected or ReservationStatus.Cancelled or ReservationStatus.Completed
                || reservation.ScheduledAt.ToUniversalTime() < now),
            InsideSevenDayWindow = futureActive.Count(reservation => reservation.ScheduledAt.ToUniversalTime() <= windowEnd),
            BeyondSevenDayWindow = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() > windowEnd),
            Changeable = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() - now >= notice),
            LockedByNotice = futureActive.Count(reservation => reservation.ScheduledAt.ToUniversalTime() - now < notice),
            PastStillOpen = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() < now),
            ApprovedAwaitingQr = reservations.Count(reservation =>
                reservation.Status == ReservationStatus.Approved && string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash)),
            QrLive = reservations.Count(reservation => IsLiveQr(reservation, now)),
            QrExpired = reservations.Count(reservation => IsExpiredQr(reservation, now)),
            QrVerified = reservations.Count(reservation =>
                reservation.Status == ReservationStatus.Approved && reservation.QrVerifiedAt.HasValue)
        };
    }

    private static NodeAnalyticsResponse ComposeNode(
        SolarStation station,
        IReadOnlyList<EnergyReservation> reservations,
        IReadOnlyDictionary<string, int> activeBySlot)
    {
        // Measure one station from its slots and the reservations that name it.
        var stationReservations = reservations.Where(reservation => reservation.StationId == station.Id).ToArray();
        var activeCount = stationReservations.Count(reservation => reservation.IsActive);
        var bookableSlots = station.Slots.Count(slot => slot.Status != SlotStatus.OutOfService);
        var committedSlots = station.Slots.Count(slot => slot.IsCommitted);
        var committedKwh = station.Slots
            .Where(slot => activeBySlot.ContainsKey(slot.Id))
            .Sum(slot => slot.BatteryCapacityKwh);

        return new NodeAnalyticsResponse
        {
            Id = station.Id,
            Code = station.Code,
            Name = station.Name,
            AddressLine = station.AddressLine,
            Latitude = station.Location.Latitude,
            Longitude = station.Location.Longitude,
            CapacityKw = station.CapacityKw,
            Status = station.Status,
            ScheduleDayCount = station.Schedule.Select(window => window.Day).Distinct().Count(),
            TotalSlots = station.TotalSlotCount,
            AvailableSlots = station.Slots.Count(slot => slot.Status == SlotStatus.Available),
            ReservedSlots = station.Slots.Count(slot => slot.Status == SlotStatus.Reserved),
            OccupiedSlots = station.Slots.Count(slot => slot.Status == SlotStatus.Occupied),
            OutOfServiceSlots = station.Slots.Count(slot => slot.Status == SlotStatus.OutOfService),
            TotalBatteryKwh = station.Slots.Sum(slot => slot.BatteryCapacityKwh),
            CommittedBatteryKwh = committedKwh,
            PendingReservations = stationReservations.Count(reservation => reservation.Status == ReservationStatus.Pending),
            ApprovedReservations = stationReservations.Count(reservation => reservation.Status == ReservationStatus.Approved),
            RejectedReservations = stationReservations.Count(reservation => reservation.Status == ReservationStatus.Rejected),
            CancelledReservations = stationReservations.Count(reservation => reservation.Status == ReservationStatus.Cancelled),
            CompletedReservations = stationReservations.Count(reservation => reservation.Status == ReservationStatus.Completed),
            ActiveReservations = activeCount,
            DeactivationBlocked = station.IsActive && activeCount > 0,
            SlotStateDrift = station.Slots.Count(slot => IsSlotDrift(slot, activeBySlot)),
            UtilizationPercent = bookableSlots == 0 ? 0 : Math.Round(committedSlots * 100d / bookableSlots, 1)
        };
    }

    private static List<BusinessRoutineResponse> ComposeRoutines(
        IReadOnlyList<SolarStation> stations,
        IReadOnlyList<EnergyBookingSlot> slots,
        IReadOnlyList<EnergyReservation> reservations,
        IReadOnlyList<User>? users,
        IReadOnlyList<Prosumer>? prosumers,
        IReadOnlyDictionary<string, EnergyBookingSlot> slotById,
        HashSet<string> stationIds,
        IReadOnlyDictionary<string, int> activeBySlot,
        DateTimeOffset now,
        DateTimeOffset windowEnd,
        TimeSpan notice,
        bool includeAccounts)
    {
        // Check each assignment rule against the records currently stored.
        var active = reservations.Where(reservation => reservation.IsActive).ToArray();
        var beyondWindow = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() > windowEnd);
        var locked = active.Count(reservation =>
            reservation.ScheduledAt.ToUniversalTime() >= now
            && reservation.ScheduledAt.ToUniversalTime() - now < notice);
        var changeable = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() - now >= notice);
        var blockedNodes = stations.Count(station =>
            station.IsActive && active.Any(reservation => reservation.StationId == station.Id));
        var inactiveWithLive = stations.Count(station =>
            !station.IsActive && active.Any(reservation => reservation.StationId == station.Id));
        var doubleBooked = activeBySlot.Count(pair => pair.Value > 1);
        var lockedSlots = slots.Count(slot => activeBySlot.ContainsKey(slot.Id));
        var drift = slots.Count(slot => IsSlotDrift(slot, activeBySlot));
        var pastOpen = active.Count(reservation => reservation.ScheduledAt.ToUniversalTime() < now);
        var awaitingQr = reservations.Count(reservation =>
            reservation.Status == ReservationStatus.Approved && string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash));
        var expiredQr = reservations.Count(reservation => IsExpiredQr(reservation, now));
        var liveQr = reservations.Count(reservation => IsLiveQr(reservation, now));
        var verifiedQr = reservations.Count(reservation =>
            reservation.Status == ReservationStatus.Approved && reservation.QrVerifiedAt.HasValue);
        var missingSchedule = stations.Count(station => station.Schedule.Count == 0);
        var orphans = reservations.Count(reservation =>
            !stationIds.Contains(reservation.StationId) || !slotById.ContainsKey(reservation.BookingSlotId));

        var routines = new List<BusinessRoutineResponse>
        {
            Routine(
                "seven-day-window",
                "Seven-day booking window",
                "A reservation must be scheduled within the next 7 days.",
                beyondWindow == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Breach,
                beyondWindow,
                beyondWindow == 0
                    ? "Every live reservation is inside the 7-day window."
                    : $"{beyondWindow} live reservations are scheduled more than 7 days ahead."),
            Routine(
                "twelve-hour-notice",
                "Twelve-hour change notice",
                "Updates and cancellations need at least 12 hours' notice.",
                locked == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                locked,
                $"{locked} live reservations are inside the 12-hour lock. {changeable} can still be updated or cancelled."),
            Routine(
                "node-deactivation",
                "Node deactivation block",
                "A microgrid node cannot be deactivated while active energy reservations exist.",
                blockedNodes == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                blockedNodes,
                blockedNodes == 0
                    ? "No active node is holding a live reservation."
                    : $"{blockedNodes} active nodes are blocked from deactivation."),
            Routine(
                "inactive-node-bookings",
                "Inactive nodes stay clear",
                "A deactivated node must not keep pending or approved reservations.",
                inactiveWithLive == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Breach,
                inactiveWithLive,
                inactiveWithLive == 0
                    ? "No inactive node still has a live reservation."
                    : $"{inactiveWithLive} inactive nodes still have live reservations."),
            Routine(
                "one-reservation-per-slot",
                "One live reservation per slot",
                "A battery slot can hold only one pending or approved reservation.",
                doubleBooked == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Breach,
                doubleBooked,
                doubleBooked == 0
                    ? "No battery slot has more than one live reservation."
                    : $"{doubleBooked} battery slots have more than one live reservation."),
            Routine(
                "slot-change-block",
                "Slot change block",
                "A battery slot with an active reservation cannot be changed or deactivated.",
                lockedSlots == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                lockedSlots,
                lockedSlots == 0
                    ? "No slot is locked by a live reservation."
                    : $"{lockedSlots} slots are locked while a live reservation holds them."),
            Routine(
                "slot-status-alignment",
                "Slot status matches reservations",
                "Reserved and occupied slots must have a live reservation, and available slots must not.",
                drift == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                drift,
                drift == 0
                    ? "Every slot status matches its live reservations."
                    : $"{drift} slots disagree with their live reservations."),
            Routine(
                "past-open-reservations",
                "Open reservations stay in the future",
                "Pending and approved reservations should still be ahead until an operator completes them.",
                pastOpen == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                pastOpen,
                pastOpen == 0
                    ? "No live reservation is already in the past."
                    : $"{pastOpen} live reservations are past their scheduled time."),
            Routine(
                "qr-dispatch",
                "QR dispatch and completion",
                "An approved reservation issues a 30-minute QR token. The operator verifies it, then completes the transfer.",
                awaitingQr + expiredQr == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                awaitingQr + expiredQr,
                $"{awaitingQr} approved reservations have no QR. {liveQr} tokens are still valid. {expiredQr} expired before verification. {verifiedQr} are verified and waiting to be completed."),
            Routine(
                "operating-schedule",
                "Weekly operating schedule",
                "Every microgrid node keeps a weekly operating schedule.",
                missingSchedule == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Attention,
                missingSchedule,
                missingSchedule == 0
                    ? "Every node has at least one operating window."
                    : $"{missingSchedule} nodes have no operating schedule."),
            Routine(
                "reservation-references",
                "Reservations point at real nodes",
                "Every reservation must name a station and battery slot that still exist.",
                orphans == 0 ? BusinessRoutineOutcome.Clear : BusinessRoutineOutcome.Breach,
                orphans,
                orphans == 0
                    ? "Every reservation names a known node and slot."
                    : $"{orphans} reservations point at a missing node or slot.")
        };

        if (includeAccounts)
        {
            routines.Add(ComposeAccountRoutine(users ?? [], prosumers ?? []));
        }

        return routines;
    }

    private static BusinessRoutineResponse ComposeAccountRoutine(
        IReadOnlyList<User> users,
        IReadOnlyList<Prosumer> prosumers)
    {
        // Report prosumer activation work that only Backoffice can finish.
        var pending = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.Pending);
        var requested = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.DeactivationRequested);
        var deactivated = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.Deactivated);
        var duplicateNics = prosumers.GroupBy(prosumer => prosumer.Nic).Count(group => group.Count() > 1);
        var outstanding = pending + requested + duplicateNics;

        return Routine(
            "prosumer-accounts",
            "Prosumer account control",
            "NIC is the prosumer key. Pending accounts wait for Backoffice. Only Backoffice can reactivate a deactivated account.",
            duplicateNics > 0
                ? BusinessRoutineOutcome.Breach
                : outstanding > 0 ? BusinessRoutineOutcome.Attention : BusinessRoutineOutcome.Clear,
            outstanding,
            $"{pending} awaiting activation. {requested} asked to be deactivated. {deactivated} are deactivated and can be reactivated only by Backoffice. {users.Count(user => user.IsActive)} web users are active.");
    }

    private static AccountAnalyticsResponse ComposeAccounts(
        IReadOnlyList<User> users,
        IReadOnlyList<Prosumer> prosumers)
    {
        // Count staff and prosumer accounts for the Backoffice view.
        return new AccountAnalyticsResponse
        {
            WebUsers = users.Count,
            ActiveWebUsers = users.Count(user => user.IsActive),
            BackofficeUsers = users.Count(user => user.Role == UserRole.Backoffice),
            GridOperatorUsers = users.Count(user => user.Role == UserRole.GridOperator),
            Prosumers = prosumers.Count,
            PendingProsumers = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.Pending),
            ActiveProsumers = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.Active),
            DeactivationRequested = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.DeactivationRequested),
            DeactivatedProsumers = prosumers.Count(prosumer => prosumer.Status == ProsumerAccountStatus.Deactivated)
        };
    }

    private static bool IsSlotDrift(EnergyBookingSlot slot, IReadOnlyDictionary<string, int> activeBySlot)
    {
        // A committed slot without a live booking, or a free slot that still has one, is drift.
        var live = activeBySlot.TryGetValue(slot.Id, out var count) ? count : 0;
        if (slot.IsCommitted)
        {
            return live == 0;
        }

        return live > 0;
    }

    private static bool IsLiveQr(EnergyReservation reservation, DateTimeOffset now)
    {
        // A live token is approved, unspent, and still inside its 30-minute life.
        return reservation.Status == ReservationStatus.Approved
            && !string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash)
            && reservation.QrVerifiedAt is null
            && reservation.QrVerificationTokenExpiresAt.HasValue
            && reservation.QrVerificationTokenExpiresAt.Value.ToUniversalTime() > now;
    }

    private static bool IsExpiredQr(EnergyReservation reservation, DateTimeOffset now)
    {
        // An expired token was issued for an approved booking and was never verified.
        return reservation.Status == ReservationStatus.Approved
            && !string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash)
            && reservation.QrVerifiedAt is null
            && reservation.QrVerificationTokenExpiresAt.HasValue
            && reservation.QrVerificationTokenExpiresAt.Value.ToUniversalTime() <= now;
    }

    private static BusinessRoutineResponse Routine(
        string code,
        string name,
        string rule,
        BusinessRoutineOutcome outcome,
        int measuredCount,
        string detail)
    {
        // Assemble one routine row for the analytics ledger.
        return new BusinessRoutineResponse
        {
            Code = code,
            Name = name,
            Rule = rule,
            Outcome = outcome,
            MeasuredCount = measuredCount,
            Detail = detail
        };
    }
}
