/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerServiceTests.cs
 * Description: Verifies prosumer registration business rules.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Services;
using SolGrid.Application.Common.Identity;
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

    [Fact]
    public async Task GetMyProsumerAsync_ReturnsOnlyAuthenticatedProsumer()
    {
        // Verify own-profile lookup uses the trusted token subject rather than client input.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(prosumer), new FakeCurrentUserContext(prosumer.Nic));

        var response = await service.GetMyProsumerAsync();

        Assert.Equal(prosumer.Nic, response.Nic);
    }

    [Fact]
    public async Task UpdateMyProsumerAsync_UpdatesEditableFieldsButPreservesNicAndStatus()
    {
        // Verify own-profile updates cannot alter immutable or administrative fields.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(prosumer), new FakeCurrentUserContext(prosumer.Nic));

        var response = await service.UpdateMyProsumerAsync(new UpdateProsumerRequest
        {
            FirstName = "Updated",
            LastName = "Prosumer",
            Email = "updated@example.com",
            PhoneNumber = "+94771234567"
        });

        Assert.Equal("199012345678", response.Nic);
        Assert.Equal(ProsumerAccountStatus.Active, response.Status);
        Assert.Equal("updated@example.com", response.Email);
    }

    [Fact]
    public async Task UpdateMyProsumerAsync_WithDuplicateEmail_ThrowsConflict()
    {
        // Verify own-profile email changes cannot claim another prosumer's email.
        var first = CreateProsumer("199012345678", "first@example.com");
        var second = CreateProsumer("199012345679", "second@example.com");
        second.Activate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(first, second), new FakeCurrentUserContext(second.Nic));

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateMyProsumerAsync(new UpdateProsumerRequest
        {
            FirstName = "Second",
            LastName = "Prosumer",
            Email = "first@example.com"
        }));
    }

    [Fact]
    public async Task RequestMyDeactivationAsync_TransitionsActiveProsumerAndRejectsRepeat()
    {
        // Verify an active prosumer can request deactivation only once.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(prosumer), new FakeCurrentUserContext(prosumer.Nic));

        await service.RequestMyDeactivationAsync();

        Assert.Equal(ProsumerAccountStatus.DeactivationRequested, prosumer.Status);
        await Assert.ThrowsAsync<ConflictException>(() => service.RequestMyDeactivationAsync());
    }

    [Fact]
    public async Task UpdateMyProsumerAsync_WithStaleTokenForInactiveProsumer_ThrowsAccountInactive()
    {
        // Verify inactive profiles cannot be mutated by a JWT issued before their lifecycle changed.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        prosumer.Deactivate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(prosumer), new FakeCurrentUserContext(prosumer.Nic));

        await Assert.ThrowsAsync<AccountInactiveException>(() => service.UpdateMyProsumerAsync(new UpdateProsumerRequest
        {
            FirstName = "Updated",
            LastName = "Prosumer",
            Email = "updated@example.com"
        }));
    }

    [Fact]
    public async Task ReservationProsumerReadService_ReturnsStableIdentifierAndActiveState()
    {
        // Verify reservation integration receives no prosumer profile or persistence-specific data.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        var service = new ReservationProsumerReadService(new InMemoryProsumerRepository(prosumer));

        var result = await service.GetByIdAsync("199012345678");

        Assert.NotNull(result);
        Assert.Equal(prosumer.Nic, result.Id);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task BackofficeLifecycle_ActivatesDeactivatesAndReactivatesProsumer()
    {
        // Verify Backoffice can perform the permitted administrative lifecycle transitions.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        var service = CreateService(new InMemoryProsumerRepository(prosumer), new FakeCurrentUserContext("backoffice-id", UserRole.Backoffice));

        await service.ActivateProsumerAsync(prosumer.Nic);
        await service.DeactivateProsumerAsync(prosumer.Nic);
        await service.ReactivateProsumerAsync(prosumer.Nic);

        Assert.Equal(ProsumerAccountStatus.Active, prosumer.Status);
    }

    [Fact]
    public async Task ReactivateProsumerAsync_WithProsumerOrGridOperator_ThrowsForbidden()
    {
        // Verify neither a prosumer nor GridOperator receives Backoffice reactivation privileges.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Deactivate(CurrentTime);
        var repository = new InMemoryProsumerRepository(prosumer);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(repository, new FakeCurrentUserContext(prosumer.Nic)).ReactivateProsumerAsync(prosumer.Nic));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(repository, new FakeCurrentUserContext("operator-id", UserRole.GridOperator)).ReactivateProsumerAsync(prosumer.Nic));
    }

    [Fact]
    public async Task GetProsumersAsync_WithPendingFilter_ReturnsPendingActivations()
    {
        // Verify Backoffice can retrieve the pending-activation management queue.
        var pending = CreateProsumer("199012345678", "pending@example.com");
        var active = CreateProsumer("199012345679", "active@example.com");
        active.Activate(CurrentTime);
        var service = CreateService(new InMemoryProsumerRepository(pending, active), new FakeCurrentUserContext("backoffice-id", UserRole.Backoffice));

        var response = await service.GetProsumersAsync(new ProsumerQuery { Status = ProsumerAccountStatus.Pending });

        var result = Assert.Single(response.Items);
        Assert.Equal(pending.Nic, result.Nic);
    }

    [Fact]
    public async Task ProsumerAuthentication_RejectsDeactivatedAndAllowsReactivatedProsumer()
    {
        // Verify shared authentication blocks inactive lifecycle states and resumes after reactivation.
        var prosumer = CreateProsumer("199012345678", "nimal@example.com");
        prosumer.Activate(CurrentTime);
        prosumer.Deactivate(CurrentTime);
        var repository = new InMemoryProsumerRepository(prosumer);
        var authService = new AuthService(new EmptyUserRepository(), repository, new FakePasswordHasher(), new FakeTokenService());

        await Assert.ThrowsAsync<AccountInactiveException>(() => authService.LoginProsumerAsync(new LoginRequest
        {
            Email = prosumer.Email,
            Password = "password"
        }));

        prosumer.Reactivate(CurrentTime);
        var response = await authService.LoginProsumerAsync(new LoginRequest { Email = prosumer.Email, Password = "password" });

        Assert.Equal("prosumer-token-199012345678", response.AccessToken);
    }

    [Fact]
    public async Task Backoffice_CanCreateAndEditPendingProfile_WithoutChangingNicOrStatus()
    {
        // Exercise administrative profile maintenance separately from activation.
        var repository = new InMemoryProsumerRepository();
        var service = CreateService(repository, new FakeCurrentUserContext("backoffice", UserRole.Backoffice));
        var created = await service.CreateProsumerAsync(CreateRequest());
        var updated = await service.UpdateProsumerAsync(created.Nic, new UpdateProsumerRequest { FirstName = "Updated", LastName = "Name", Email = "updated@example.com", PhoneNumber = "+94712345678" });
        Assert.Equal(created.Nic, updated.Nic);
        Assert.Equal(ProsumerAccountStatus.Pending, updated.Status);
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal("updated@example.com", (await repository.GetByNicAsync(created.Nic))!.Email);
    }

    [Fact]
    public async Task GridOperator_CannotCreateOrEditProsumerProfiles()
    {
        // Verify the service rejects administrative writes even outside the controller.
        var service = CreateService(currentUserContext: new FakeCurrentUserContext("operator", UserRole.GridOperator));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateProsumerAsync(CreateRequest()));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateProsumerAsync("199012345678", new UpdateProsumerRequest()));
    }

    [Fact]
    public async Task Backoffice_EditCannotReuseAnotherProsumerEmail()
    {
        // Preserve email uniqueness on administrative updates.
        var first = CreateProsumer("199012345678", "first@example.com");
        var second = CreateProsumer("199012345679", "second@example.com");
        var service = CreateService(new InMemoryProsumerRepository(first, second), new FakeCurrentUserContext("backoffice", UserRole.Backoffice));
        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateProsumerAsync(first.Nic, new UpdateProsumerRequest { FirstName = "First", LastName = "Name", Email = second.Email, PhoneNumber = "+94712345678" }));
        Assert.Equal("first@example.com", first.Email);
    }

    private static ProsumerService CreateService(InMemoryProsumerRepository? repository = null, FakeCurrentUserContext? currentUserContext = null)
    {
        // Create a prosumer service with deterministic dependencies for registration tests.
        var activeRepository = repository ?? new InMemoryProsumerRepository();
        if (currentUserContext is null)
        {
            return new ProsumerService(activeRepository, new EmptyUserRepository(), new FakePasswordHasher(), new FixedTimeProvider(CurrentTime));
        }

        return new ProsumerService(activeRepository, new EmptyUserRepository(), new FakePasswordHasher(), currentUserContext, new FixedTimeProvider(CurrentTime));
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

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken CreateToken(User user)
        {
            // Return a deterministic web-user token for interface completeness in prosumer tests.
            return new IssuedToken { AccessToken = "web-user-token", ExpiresAt = CurrentTime.AddHours(1) };
        }

        public IssuedToken CreateProsumerToken(string nic)
        {
            // Return a deterministic prosumer token for shared-authentication tests.
            return new IssuedToken { AccessToken = $"prosumer-token-{nic}", ExpiresAt = CurrentTime.AddHours(1) };
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
            var filteredProsumers = query.Status.HasValue
                ? prosumers.Where(prosumer => prosumer.Status == query.Status.Value).ToArray()
                : prosumers.ToArray();
            return Task.FromResult(new PagedResult<Prosumer>
            {
                Items = filteredProsumers,
                TotalCount = filteredProsumers.Length,
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

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(string userId, UserRole? role = null)
        {
            // Store trusted current-user identity values for application ownership tests.
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated => true;

        public string? UserId { get; }

        public UserRole? Role { get; }
    }

    private sealed class EmptyUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Return no web user because prosumer authentication does not query this test double.
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Return no web user because prosumer authentication does not query this test double.
            return Task.FromResult<User?>(null);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return no web users from the prosumer-authentication test double.
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }

        public Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
        {
            // Return an empty user page from the prosumer-authentication test double.
            return Task.FromResult(new PagedResult<User>());
        }

        public Task<bool> ExistsByEmailAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Report no web-user email conflicts from the prosumer-authentication test double.
            return Task.FromResult(false);
        }

        public Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Report no legacy web-user email conflicts from the prosumer-authentication test double.
            return Task.FromResult(false);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            // Complete without persistence because this test double is never written.
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            // Complete without persistence because this test double is never written.
            return Task.CompletedTask;
        }
    }
}
