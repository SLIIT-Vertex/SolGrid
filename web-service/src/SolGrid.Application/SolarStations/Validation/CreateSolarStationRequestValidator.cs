/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CreateSolarStationRequestValidator.cs
 * Description: Validates requests that register a new microgrid solar station.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class CreateSolarStationRequestValidator : IRequestValidator<CreateSolarStationRequest>
{
    public ValidationResult Validate(CreateSolarStationRequest request)
    {
        // Collect every station creation problem so clients can fix them in one round trip.
        if (request is null)
        {
            return ValidationResult.Failure(["Station details are required."]);
        }

        var errors = SolarStationValidationRules
            .ValidateIdentity(request.Code, request.Name, request.AddressLine)
            .Concat(SolarStationValidationRules.ValidateLocation(request.Location))
            .Concat(SolarStationValidationRules.ValidateCapacity(request.CapacityKw))
            .Concat(SolarStationValidationRules.ValidateSlots(request.Slots))
            .Concat(SolarStationValidationRules.ValidateSchedule(request.Schedule))
            .ToArray();

        return errors.Length == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
