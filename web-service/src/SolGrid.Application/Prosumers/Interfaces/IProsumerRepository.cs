/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IProsumerRepository.cs
 * Description: Defines persistence operations required by prosumer application services.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Prosumers.Interfaces;

public interface IProsumerRepository
{
    Task<Prosumer?> GetByNicAsync(string nic, CancellationToken cancellationToken = default);

    Task<Prosumer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<PagedResult<Prosumer>> GetPagedAsync(ProsumerQuery query, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNicAsync(string nic, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, string? excludingNic = null, CancellationToken cancellationToken = default);

    Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default);
}

public sealed class ProsumerQuery
{
    public string? SearchText { get; init; }

    public ProsumerAccountStatus? Status { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
