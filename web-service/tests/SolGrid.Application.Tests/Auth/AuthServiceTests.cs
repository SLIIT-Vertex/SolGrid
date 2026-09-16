/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthServiceTests.cs
 * Description: Verifies web user authentication business rules.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithBackofficeCredentials_ReturnsToken()
    {
        // Verify Backoffice users can authenticate with valid credentials.
        var user = CreateUser("backoffice@example.com", "password123", UserRole.Backoffice);
        var service = CreateService(user);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "BACKOFFICE@example.com",
            Password = "password123"
        });

        Assert.Equal("token-user-1-Backoffice", response.AccessToken);
        Assert.Equal(UserRole.Backoffice, response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WithGridOperatorCredentials_ReturnsToken()
    {
        // Verify GridOperator users can authenticate with valid credentials.
        var user = CreateUser("operator@example.com", "operator123", UserRole.GridOperator);
        var service = CreateService(user);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "operator@example.com",
            Password = "operator123"
        });

        Assert.Equal("token-user-1-GridOperator", response.AccessToken);
        Assert.Equal(UserRole.GridOperator, response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsAuthenticationFailed()
    {
        // Verify invalid passwords are rejected without issuing a token.
        var user = CreateUser("user@example.com", "correct", UserRole.Backoffice);
        var service = CreateService(user);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsAuthenticationFailed()
    {
        // Verify unknown emails are rejected with the same safe authentication error.
        var service = CreateService();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "password123"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsAccountInactive()
    {
        // Verify inactive accounts cannot receive access tokens.
        var user = CreateUser("inactive@example.com", "password123", UserRole.Backoffice);
        user.Deactivate(DateTimeOffset.UtcNow);
        var service = CreateService(user);

        await Assert.ThrowsAsync<AccountInactiveException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "password123"
        }));
    }

    private static AuthService CreateService(params User[] users)
    {
        // Create AuthService with deterministic test doubles.
        return new AuthService(
            new InMemoryUserRepository(users),
            new FakePasswordHasher(),
            new FakeTokenService());
    }

    private static User CreateUser(string email, string password, UserRole role)
    {
        // Create a domain user with a fake stored password hash.
        return User.Create(
            "user-1",
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

        public InMemoryUserRepository(IEnumerable<User> users)
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
            // Return all in-memory users in a single page.
            return Task.FromResult(new PagedResult<User>
            {
                Items = users,
                TotalCount = users.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<bool> ExistsByEmailAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Check whether an email exists in memory.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(users.Any(user => user.Email == normalizedEmail && user.Id != excludingUserId));
        }

        public Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
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
            // Replace is not required for these authentication tests.
            return Task.CompletedTask;
        }
    }
}
