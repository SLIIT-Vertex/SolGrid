/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IUserRepository.cs
 * Description: Defines persistence operations required by web user application services.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Entities;

namespace SolGrid.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
