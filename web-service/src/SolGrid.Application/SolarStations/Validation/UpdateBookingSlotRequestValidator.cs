/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateBookingSlotRequestValidator.cs
 * Description: Validates booking slot capacity and absolute time ranges.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class UpdateBookingSlotRequestValidator : IRequestValidator<UpdateBookingSlotRequest>
{
    public ValidationResult Validate(UpdateBookingSlotRequest request)
    {
        // Collect editable-field errors before any slot state is changed.
        if (request is null)
        {
            return ValidationResult.Failure(["Booking slot details are required."]);
        }

        var errors = SolarStationValidationRules.ValidateSlotDetails(
            request.BatteryCapacityKwh, request.StartTime, request.EndTime).ToArray();
        return errors.Length == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
