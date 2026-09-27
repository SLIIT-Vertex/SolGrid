/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ResetPasswordRequest.cs
 * Description: Carries a new password for a Backoffice-initiated web user password reset.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Users.Requests;

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}
