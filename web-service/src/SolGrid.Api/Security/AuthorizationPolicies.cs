/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthorizationPolicies.cs
 * Description: Defines authorization policy names for SolGrid API roles.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Api.Security;

public static class AuthorizationPolicies
{
    public const string Backoffice = nameof(Backoffice);

    public const string GridOperator = nameof(GridOperator);
}
