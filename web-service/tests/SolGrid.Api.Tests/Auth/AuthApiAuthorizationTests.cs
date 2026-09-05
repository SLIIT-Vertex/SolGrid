/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthApiAuthorizationTests.cs
 * Description: Verifies API authentication and role authorization behavior.
 * Contributor: Bawanthi K D R
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SolGrid.Api.Security;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Xunit;

namespace SolGrid.Api.Tests.Auth;

public sealed class AuthApiAuthorizationTests
{
    private const string JwtSigningKey = "test-signing-key-for-solgrid-auth-tests-32";

    [Fact]
    public async Task ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify protected endpoints reject unauthenticated requests.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RolePolicies_AllowOnlyMatchingRole()
    {
        // Verify role policies are enforced server-side from role claims.
        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var backofficePrincipal = CreatePrincipal("backoffice-id", UserRole.Backoffice);

        var allowedResult = await authorizationService.AuthorizeAsync(
            backofficePrincipal,
            resource: null,
            AuthorizationPolicies.Backoffice);
        var forbiddenResult = await authorizationService.AuthorizeAsync(
            backofficePrincipal,
            resource: null,
            AuthorizationPolicies.GridOperator);

        Assert.True(allowedResult.Succeeded);
        Assert.False(forbiddenResult.Succeeded);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesBearerSecurityScheme()
    {
        // Verify OpenAPI metadata advertises JWT bearer authentication.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var openApiJson = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"Bearer\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"bearer\"", openApiJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserAdministration_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator users cannot perform Backoffice-only user administration.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "operator@example.com", "operator-password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            FirstName = "New",
            LastName = "Operator",
            Email = "new.operator@example.com",
            Password = "password123",
            Role = UserRole.GridOperator
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserAdministration_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify user administration endpoints require authentication.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserAdministration_WithBackofficeJwt_CreatesUserWithoutPasswordHashResponse()
    {
        // Verify Backoffice users can create accounts and responses hide password hashes.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "backoffice@example.com", "backoffice-password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            FirstName = "New",
            LastName = "Operator",
            Email = "new.operator@example.com",
            Password = "password123",
            Role = UserRole.GridOperator
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password123", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserAdministration_WithBackofficeJwtAndMissingUser_ReturnsNotFoundProblem()
    {
        // Verify missing users return a consistent 404 ProblemDetails response.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "backoffice@example.com", "backoffice-password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/missing-id");
        var contentType = response.Content.Headers.ContentType?.MediaType;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task CorsPreflight_FromConfiguredReactOrigin_ReturnsCorsHeaders()
    {
        // Verify configured browser clients can pass CORS preflight checks.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:5173", origins);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        // Create an API test host with deterministic auth configuration and users.
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = "SolGrid.Tests",
                        ["Jwt:Audience"] = "SolGrid.Tests",
                        ["Jwt:SigningKey"] = JwtSigningKey,
                        ["Jwt:ExpiresMinutes"] = "30",
                        ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                        ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:1",
                        ["MongoDb:DatabaseName"] = "SolGridTests",
                        ["MongoDb:UsersCollectionName"] = "Users"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserRepository>();
                    services.AddSingleton<IUserRepository>(new TestUserRepository());
                    services.RemoveAll<IAuthService>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.RemoveAll<IUserService>();
                    services.AddScoped<IUserService, UserService>();
                });
            });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        // Request a JWT through the public login endpoint.
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LoginBody>();
        return content!.AccessToken;
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, UserRole role)
    {
        // Create an authenticated principal with SolGrid JWT-equivalent role claims.
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    private sealed class LoginBody
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class TestUserRepository : IUserRepository
    {
        private readonly List<User> users =
        [
            CreateUser("backoffice-id", "backoffice@example.com", "backoffice-password", UserRole.Backoffice),
            CreateUser("operator-id", "operator@example.com", "operator-password", UserRole.GridOperator)
        ];

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a test user by id.
            return Task.FromResult(users.FirstOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Find a test user by normalized email.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(users.FirstOrDefault(user => user.Email == normalizedEmail));
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return all test users.
            return Task.FromResult<IReadOnlyList<User>>(users);
        }

        public Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
        {
            // Return all test users in a single page.
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
            // Check whether an email exists in test data.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(users.Any(user => user.Email == normalizedEmail && user.Id != excludingUserId));
        }

        public Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Preserve repository compatibility in API tests.
            return ExistsByEmailAsync(email, excludingUserId, cancellationToken);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            // Add a user to test data.
            users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            // Updates are not needed for API auth tests.
            return Task.CompletedTask;
        }

        private static User CreateUser(string id, string email, string password, UserRole role)
        {
            // Create a test user with a BCrypt password hash.
            return User.Create(
                id,
                "Test",
                "User",
                email,
                BCrypt.Net.BCrypt.HashPassword(password),
                role,
                DateTimeOffset.UtcNow);
        }
    }
}
