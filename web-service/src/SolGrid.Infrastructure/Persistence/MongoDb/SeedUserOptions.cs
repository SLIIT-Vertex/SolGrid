/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SeedUserOptions.cs
 * Description: Holds non-secret development seed user configuration.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public sealed class SeedUserOptions
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
