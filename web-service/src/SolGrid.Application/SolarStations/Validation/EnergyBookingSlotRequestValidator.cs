/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlotRequestValidator.cs
 * Description: Validates requests that add battery storage slots to a solar station.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class EnergyBookingSlotRequestValidator : IRequestValidator<EnergyBookingSlotRequest>
{
    public ValidationResult Validate(EnergyBookingSlotRequest request)
    {
        // Validate one slot definition before it joins the station aggregate.
        var errors = SolarStationValidationRules.ValidateSlot(request).ToArray();

        return errors.Length == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
