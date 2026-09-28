/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ActiveProsumerRepository.cs
 * Description: Supplies the existing mobile test identity for live JWT account-status checks.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Domain.Entities;

namespace SolGrid.Api.Tests;

internal sealed class ActiveProsumerRepository : IProsumerRepository
{
    public Task<Prosumer?> GetByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Keep station/slot hosts isolated from MongoDB while checking a real account lifecycle state.
        if (nic != "199012345678") return Task.FromResult<Prosumer?>(null);
        var prosumer = Prosumer.Create(nic, "Test", "Prosumer", "test@example.com", null, "unused-hash", DateTimeOffset.UtcNow);
        prosumer.Activate(DateTimeOffset.UtcNow);
        return Task.FromResult<Prosumer?>(prosumer);
    }

    // These station fixtures must never read or mutate other prosumer data.
    public Task<Prosumer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PagedResult<Prosumer>> GetPagedAsync(ProsumerQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> ExistsByNicAsync(string nic, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> ExistsByEmailAsync(string email, string? excludingNic = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
