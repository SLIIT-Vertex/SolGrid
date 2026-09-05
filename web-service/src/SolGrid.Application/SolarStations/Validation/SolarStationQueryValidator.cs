/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationQueryValidator.cs
 * Description: Validates paged microgrid query parameters.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Interfaces;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class SolarStationQueryValidator : IRequestValidator<SolarStationQuery>
{
    public ValidationResult Validate(SolarStationQuery request)
    {
        // Reject unsupported filters and invalid paging before querying persistence.
        var errors = SolarStationValidationRules.ValidateStationQuery(request).ToArray();
        return errors.Length == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
