/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IAuthService.cs
 * Description: Defines authentication use cases for web users.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;

namespace SolGrid.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ProsumerLoginResponse> LoginProsumerAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
