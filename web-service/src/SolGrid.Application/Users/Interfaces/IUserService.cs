/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IUserService.cs
 * Description: Defines web user management use cases.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.Users.Requests;
using SolGrid.Application.Users.Responses;

namespace SolGrid.Application.Users.Interfaces;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<UserResponse>> GetUsersAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task ActivateUserAsync(string id, CancellationToken cancellationToken = default);

    Task ReactivateUserAsync(string id, CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(string id, CancellationToken cancellationToken = default);
}
