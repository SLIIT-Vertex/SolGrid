/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateSlotStatusRequest.cs
 * Description: Carries an operator requested battery storage slot state change.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Requests;

public sealed class UpdateSlotStatusRequest
{
    public SlotStatus Status { get; init; }
}
