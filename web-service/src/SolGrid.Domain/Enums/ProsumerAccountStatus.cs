/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerAccountStatus.cs
 * Description: Defines the lifecycle states for solar prosumer accounts.
 * Contributor: Gunasekara H N
 */

namespace SolGrid.Domain.Enums;

public enum ProsumerAccountStatus
{
    Pending = 1,
    Active = 2,
    DeactivationRequested = 3,
    Deactivated = 4
}
