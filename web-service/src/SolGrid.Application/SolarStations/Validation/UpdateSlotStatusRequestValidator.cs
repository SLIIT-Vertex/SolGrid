/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateSlotStatusRequestValidator.cs
 * Description: Validates the requested slot state before domain transition checks.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Validation;
using SolGrid.Application.SolarStations.Requests;

namespace SolGrid.Application.SolarStations.Validation;

public sealed class UpdateSlotStatusRequestValidator : IRequestValidator<UpdateSlotStatusRequest>
{
    public ValidationResult Validate(UpdateSlotStatusRequest request)
    {
        // Reject missing or undefined states; the loaded entity enforces the transition.
        return request is not null && Enum.IsDefined(request.Status)
            ? ValidationResult.Success()
            : ValidationResult.Failure(["A supported slot status is required."]);
    }
}
