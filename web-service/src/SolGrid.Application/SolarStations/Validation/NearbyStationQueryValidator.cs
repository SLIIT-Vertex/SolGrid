/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: NearbyStationQueryValidator.cs
 * Description: Validates Android Maps nearby-station search parameters.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Interfaces;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class NearbyStationQueryValidator : IRequestValidator<NearbyStationQuery>
{
    public ValidationResult Validate(NearbyStationQuery request)
    {
        // Reject coordinates and search bounds that cannot drive a MongoDB geo query.
        var errors = SolarStationValidationRules.ValidateNearbyQuery(request).ToArray();
        return errors.Length == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
