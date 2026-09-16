/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateSolarStationRequestValidator.cs
 * Description: Validates requests that edit an existing microgrid solar station.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class UpdateSolarStationRequestValidator : IRequestValidator<UpdateSolarStationRequest>
{
    public ValidationResult Validate(UpdateSolarStationRequest request)
    {
        // Slots and schedules have their own endpoints, so only descriptive fields are checked here.
        if (request is null)
        {
            return ValidationResult.Failure(["Station details are required."]);
        }

        var errors = SolarStationValidationRules
            .ValidateIdentity(request.Code, request.Name, request.AddressLine)
            .Concat(SolarStationValidationRules.ValidateLocation(request.Location))
            .Concat(SolarStationValidationRules.ValidateCapacity(request.CapacityKw))
            .ToArray();

        return errors.Length == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
