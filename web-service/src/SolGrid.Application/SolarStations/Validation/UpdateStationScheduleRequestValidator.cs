/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateStationScheduleRequestValidator.cs
 * Description: Validates replacement weekly schedules for a microgrid solar station.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class UpdateStationScheduleRequestValidator : IRequestValidator<UpdateStationScheduleRequest>
{
    public ValidationResult Validate(UpdateStationScheduleRequest request)
    {
        // Reject schedules that would leave reservations unable to resolve booking windows.
        if (request is null)
        {
            return ValidationResult.Failure(["Schedule details are required."]);
        }

        var errors = SolarStationValidationRules.ValidateSchedule(request.Schedule).ToArray();

        return errors.Length == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
