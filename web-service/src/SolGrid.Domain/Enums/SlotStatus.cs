/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SlotStatus.cs
 * Description: Defines battery storage slot states used by energy booking.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Domain.Enums;

public enum SlotStatus
{
    Available = 1,
    Reserved = 2,
    Occupied = 3,
    OutOfService = 4
}
