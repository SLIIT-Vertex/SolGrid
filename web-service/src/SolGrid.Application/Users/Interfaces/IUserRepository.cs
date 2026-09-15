/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IUserRepository.cs
 * Description: Defines persistence operations required by web user application services.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

public sealed class UserQuery
{
    public string? SearchText { get; init; }

    public UserRole? Role { get; init; }

    public AccountStatus? Status { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public long TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }
}
