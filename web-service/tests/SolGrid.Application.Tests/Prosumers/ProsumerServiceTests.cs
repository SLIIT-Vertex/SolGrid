/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerServiceTests.cs
 * Description: Verifies prosumer registration business rules.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Services;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using System.Text.Json;
using Xunit;

namespace SolGrid.Application.Tests.Prosumers;

public sealed class ProsumerServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegisterProsumerAsync_WithValidRequest_CreatesPendingProsumerWithHashedPassword()
    {
        // Verify registration normalizes profile values, hashes the password, and starts pending.
        var repository = new InMemoryProsumerRepository();
        var service = CreateService(repository);

        var response = await service.RegisterProsumerAsync(CreateRequest());
        var persistedProsumer = await repository.GetByNicAsync(response.Nic);

        Assert.NotNull(persistedProsumer);
        Assert.Equal("199012345678", response.Nic);
        Assert.Equal("nimal@example.com", response.Email);
        Assert.Equal(ProsumerAccountStatus.Pending, response.Status);
        Assert.Equal("hash:password123", persistedProsumer!.PasswordHash);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password123", JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterProsumerAsync_WithDuplicateNic_ThrowsConflict()
    {
        // Verify normalized duplicate NIC values are rejected before persistence.
        var repository = new InMemoryProsumerRepository(CreateProsumer("199012345678", "first@example.com"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterProsumerAsync(CreateRequest()));
    }

    [Fact]
    public async Task RegisterProsumerAsync_WithDuplicateEmail_ThrowsConflict()
    {
        // Verify normalized duplicate email values are rejected before persistence.
        var repository = new InMemoryProsumerRepository(CreateProsumer("199012345679", "nimal@example.com"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterProsumerAsync(CreateRequest()));
    }

    [Fact]
    public async Task RegisterProsumerAsync_WithInvalidNic_ThrowsValidation()
    {
        // Verify unsupported NIC shapes are rejected by the registration contract.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.RegisterProsumerAsync(CreateRequest(nic: "invalid-nic")));
    }

    [Fact]
    public async Task RegisterProsumerAsync_WithInvalidEmail_ThrowsValidation()
    {
        // Verify invalid email values are rejected before repository access.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.RegisterProsumerAsync(CreateRequest(email: "invalid-email")));
    }

    private static ProsumerService CreateService(InMemoryProsumerRepository? repository = null)
    {
        // Create a prosumer service with deterministic dependencies for registration tests.
        return new ProsumerService(
            repository ?? new InMemoryProsumerRepository(),
            new FakePasswordHasher(),
            new FixedTimeProvider(CurrentTime));
    }

    private static RegisterProsumerRequest CreateRequest(
        string nic = "199012345678",
        string email = "NIMAL@example.com")
    {
        // Create a valid prosumer registration request for service tests.
        return new RegisterProsumerRequest
        {
            Nic = nic,
            FirstName = "Nimal",
            LastName = "Perera",
            Email = email,
            PhoneNumber = "+94712345678",
            Password = "password123"
        };
    }

    private static Prosumer CreateProsumer(string nic, string email)
    {
        // Create a persisted-style prosumer for duplicate-registration tests.
        return Prosumer.Create(
            nic,
            "Existing",
            "Prosumer",
            email,
            null,
            "hash:password",
            CurrentTime);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Return a deterministic non-plaintext password hash for service tests.
            return $"hash:{password}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            // Verify deterministic test hashes when a future test needs credential checks.
            return passwordHash == HashPassword(password);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset currentTime;

        public FixedTimeProvider(DateTimeOffset currentTime)
        {
            // Store the deterministic UTC timestamp used by registration tests.
            this.currentTime = currentTime;
        }

        public override DateTimeOffset GetUtcNow()
        {
            // Return the deterministic UTC timestamp used by registration tests.
            return currentTime;
        }
    }

    private sealed class InMemoryProsumerRepository : IProsumerRepository
    {
        private readonly List<Prosumer> prosumers;

        public InMemoryProsumerRepository(params Prosumer[] prosumers)
        {
            // Store test prosumers in memory.
            this.prosumers = prosumers.ToList();
        }

        public Task<Prosumer?> GetByNicAsync(string nic, CancellationToken cancellationToken = default)
        {
            // Find a test prosumer by normalized NIC.
            return Task.FromResult(prosumers.FirstOrDefault(prosumer => prosumer.Nic == nic.Trim().ToUpperInvariant()));
        }

        public Task<Prosumer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Find a test prosumer by normalized email.
            return Task.FromResult(prosumers.FirstOrDefault(prosumer => prosumer.Email == email.Trim().ToLowerInvariant()));
        }

        public Task<PagedResult<Prosumer>> GetPagedAsync(ProsumerQuery query, CancellationToken cancellationToken = default)
        {
            // Return a simple page for future-contract compatibility in registration tests.
            return Task.FromResult(new PagedResult<Prosumer>
            {
                Items = prosumers,
                TotalCount = prosumers.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<bool> ExistsByNicAsync(string nic, CancellationToken cancellationToken = default)
        {
            // Check normalized NIC uniqueness in test storage.
            return Task.FromResult(prosumers.Any(prosumer => prosumer.Nic == nic.Trim().ToUpperInvariant()));
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            string? excludingNic = null,
            CancellationToken cancellationToken = default)
        {
            // Check normalized email uniqueness in test storage.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(prosumers.Any(prosumer =>
                prosumer.Email == normalizedEmail && prosumer.Nic != excludingNic));
        }

        public Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Add a prosumer to test storage.
            prosumers.Add(prosumer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Preserve future repository-contract compatibility without update test behavior.
            return Task.CompletedTask;
        }
    }
}
