/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserServiceTests.cs
 * Description: Verifies web user management business use cases.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Requests;
using SolGrid.Application.Users.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Application.Tests.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_WithBackofficeRole_CreatesBackofficeUser()
    {
        // Verify Backoffice users can be created with hashed passwords.
        var repository = new InMemoryUserRepository();
        var service = CreateUserService(repository);

        var response = await service.CreateUserAsync(new CreateUserRequest
        {
            FirstName = "Back",
            LastName = "Office",
            Email = "BACKOFFICE@example.com",
            Password = "password123",
            Role = UserRole.Backoffice
        });

        var storedUser = await repository.GetByIdAsync(response.Id);
        Assert.Equal(UserRole.Backoffice, response.Role);
        Assert.Equal("backoffice@example.com", response.Email);
        Assert.Equal("hash:password123", storedUser!.PasswordHash);
    }

    [Fact]
    public async Task CreateUserAsync_WithGridOperatorRole_CreatesGridOperatorUser()
    {
        // Verify GridOperator users can be created with the supported role.
        var service = CreateUserService();

        var response = await service.CreateUserAsync(new CreateUserRequest
        {
            FirstName = "Grid",
            LastName = "Operator",
            Email = "operator@example.com",
            Password = "operator123",
            Role = UserRole.GridOperator
        });

        Assert.Equal(UserRole.GridOperator, response.Role);
        Assert.Equal(AccountStatus.Active, response.Status);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ThrowsConflict()
    {
        // Verify duplicate normalized emails are rejected.
        var repository = new InMemoryUserRepository(CreateUser("existing-id", "existing@example.com", "password", UserRole.Backoffice));
        var service = CreateUserService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateUserAsync(new CreateUserRequest
        {
            FirstName = "Another",
            LastName = "User",
            Email = "EXISTING@example.com",
            Password = "password123",
            Role = UserRole.GridOperator
        }));
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidRole_ThrowsValidation()
    {
        // Verify unsupported role values are rejected before persistence.
        var service = CreateUserService();

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateUserAsync(new CreateUserRequest
        {
            FirstName = "Invalid",
            LastName = "Role",
            Email = "invalid@example.com",
            Password = "password123",
            Role = (UserRole)999
        }));
    }

    [Fact]
    public async Task UpdateUserAsync_WithEditableFields_UpdatesUser()
    {
        // Verify profile fields and role are updated while status remains a separate workflow.
        var user = CreateUser("user-id", "old@example.com", "password", UserRole.GridOperator);
        var repository = new InMemoryUserRepository(user);
        var service = CreateUserService(repository);

        var response = await service.UpdateUserAsync("user-id", new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Person",
            Email = "UPDATED@example.com",
            Role = UserRole.Backoffice
        });

        Assert.Equal("Updated", response.FirstName);
        Assert.Equal("updated@example.com", response.Email);
        Assert.Equal(UserRole.Backoffice, response.Role);
        Assert.Equal(AccountStatus.Active, response.Status);
    }

    [Fact]
    public async Task DeactivateUserAsync_ChangesStatusAndRejectsLogin()
    {
        // Verify deactivation is a business status change that blocks authentication.
        var user = CreateUser("user-id", "user@example.com", "password", UserRole.Backoffice);
        var repository = new InMemoryUserRepository(user);
        var userService = CreateUserService(repository);
        var authService = CreateAuthService(repository);

        await userService.DeactivateUserAsync("user-id");

        Assert.Equal(AccountStatus.Inactive, user.Status);
        await Assert.ThrowsAsync<AccountInactiveException>(() => authService.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "password"
        }));
    }

    [Fact]
    public async Task ReactivateUserAsync_ChangesStatusAndAllowsLogin()
    {
        // Verify reactivation restores authentication eligibility.
        var user = CreateUser("user-id", "user@example.com", "password", UserRole.GridOperator);
        user.Deactivate(DateTimeOffset.UtcNow);
        var repository = new InMemoryUserRepository(user);
        var userService = CreateUserService(repository);
        var authService = CreateAuthService(repository);

        await userService.ReactivateUserAsync("user-id");
        var response = await authService.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "password"
        });

        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal("token-user-id-GridOperator", response.AccessToken);
    }

    private static UserService CreateUserService(InMemoryUserRepository? repository = null)
    {
        // Create UserService with deterministic test dependencies.
        return new UserService(repository ?? new InMemoryUserRepository(), new FakePasswordHasher());
    }

    private static AuthService CreateAuthService(InMemoryUserRepository repository)
    {
        // Create AuthService with deterministic test dependencies.
        return new AuthService(repository, new FakePasswordHasher(), new FakeTokenService());
    }

    private static User CreateUser(string id, string email, string password, UserRole role)
    {
        // Create a domain user with a fake stored password hash.
        return User.Create(
            id,
            "Test",
            "User",
            email,
            $"hash:{password}",
            role,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Create a deterministic fake hash for tests.
            return $"hash:{password}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            // Verify the deterministic fake hash.
            return passwordHash == HashPassword(password);
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken CreateToken(User user)
        {
            // Create a deterministic fake token for tests.
            return new IssuedToken
            {
                AccessToken = $"token-{user.Id}-{user.Role}",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            };
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> users;

        public InMemoryUserRepository(params User[] users)
        {
            // Store test users in memory.
            this.users = users.ToList();
        }

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a user by id in memory.
            return Task.FromResult(users.FirstOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Find a user by normalized email in memory.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(users.FirstOrDefault(user => user.Email == normalizedEmail));
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return all in-memory users.
            return Task.FromResult<IReadOnlyList<User>>(users);
        }

        public Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
        {
            // Return filtered in-memory users for service tests.
            IEnumerable<User> queryableUsers = users;

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                queryableUsers = queryableUsers.Where(user =>
                    user.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || user.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || user.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Role.HasValue)
            {
                queryableUsers = queryableUsers.Where(user => user.Role == query.Role.Value);
            }

            if (query.Status.HasValue)
            {
                queryableUsers = queryableUsers.Where(user => user.Status == query.Status.Value);
            }

            var items = queryableUsers.ToArray();
            return Task.FromResult(new PagedResult<User>
            {
                Items = items,
                TotalCount = items.Length,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            string? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            // Check whether an email exists in memory.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(users.Any(user => user.Email == normalizedEmail && user.Id != excludingUserId));
        }

        public Task<bool> EmailExistsAsync(
            string email,
            string? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            // Preserve repository compatibility in tests.
            return ExistsByEmailAsync(email, excludingUserId, cancellationToken);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            // Add a user to the in-memory list.
            users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            // Replace is not required because domain users are updated in place for tests.
            return Task.CompletedTask;
        }
    }
}
