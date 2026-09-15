/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ICurrentUserContext.cs
 * Description: Defines the current authenticated user without depending on HTTP infrastructure.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Common.Identity;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    UserRole? Role { get; }
}
