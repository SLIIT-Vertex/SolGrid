/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IRequestValidator.cs
 * Description: Defines request validation contracts for application use cases.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Validation;

public interface IRequestValidator<in TRequest>
{
    ValidationResult Validate(TRequest request);
}
