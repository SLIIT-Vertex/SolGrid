/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationStatus.cs
 * Description: Defines energy reservation lifecycle states.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Domain.Enums;

public enum ReservationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Completed = 5
}
