/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerApiTests.cs
 * Description: Verifies public prosumer registration API behavior.
 * Contributor: Gunasekara H N
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SolGrid.Api.Tests.Prosumers;

public sealed class ProsumerApiTests
{
    private const string TestJwtSigningKey = "test-signing-key-for-solgrid-auth-tests-32";

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedPendingProsumerWithoutPasswordHash()
    {
        // Verify public registration returns a safe pending profile response.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/prosumers/register", new
        {
            Nic = "199012345678",
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal@example.com",
            PhoneNumber = "0712345678",
            Password = "password123"
        });
        var body = await response.Content.ReadAsStringAsync();
        using var responseDocument = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, responseDocument.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password123", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithInvalidNic_ReturnsBadRequestProblem()
    {
        // Verify invalid NIC input is mapped by centralized validation handling.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/prosumers/register", new
        {
            Nic = "invalid-nic",
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal@example.com",
            Password = "password123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesProsumerRegistrationEndpoint()
    {
        // Verify OpenAPI advertises the public prosumer registration contract.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var openApiJson = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"/api/v1/prosumers/register\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/login\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/me\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/me/request-deactivation\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/pending\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/{nic}\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/{nic}/activate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/{nic}/deactivate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/prosumers/{nic}/reactivate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"post\"", openApiJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProsumers_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify Backoffice management data cannot be obtained anonymously.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/prosumers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProsumers_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot satisfy the Backoffice prosumer-administration policy.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.GetAsync("/api/v1/prosumers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Backoffice_CanCreateAndEditProfile_ThroughAdministrativeRoutes()
    {
        // Verify the web management REST contract and immutable account identity.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateWebUserJwt(UserRole.Backoffice));
        var created = await client.PostAsJsonAsync("/api/v1/prosumers", new { Nic = "199012345678", FirstName = "First", LastName = "Name", Email = "created@example.com", Password = "password123" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var updated = await client.PutAsJsonAsync("/api/v1/prosumers/199012345678", new { FirstName = "Updated", LastName = "Name", Email = "updated@example.com" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = await updated.Content.ReadAsStringAsync();
        Assert.Contains("updated@example.com", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GridOperator_CannotWriteAdministrativeProsumerRoutes()
    {
        // Check HTTP authorization before administrative services execute.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateWebUserJwt(UserRole.GridOperator));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/prosumers", new { Nic = "199012345678" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/v1/prosumers/199012345678", new { FirstName = "Updated" })).StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        // Create an API test host with Mongo startup initialization disabled.
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = "SolGrid.Tests",
                        ["Jwt:Audience"] = "SolGrid.Tests",
                        ["Jwt:SigningKey"] = TestJwtSigningKey,
                        ["Jwt:ExpiresMinutes"] = "30",
                        ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:1",
                        ["MongoDb:DatabaseName"] = "SolGridTests",
                        ["MongoDb:InitializeOnStartup"] = "false"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IReservationService>();
                    services.RemoveAll<IUserRepository>();
                    services.AddSingleton<IUserRepository>(new TestUserRepository());
                    services.RemoveAll<IProsumerRepository>();
                    services.AddSingleton<IProsumerRepository>(new TestProsumerRepository());
                });
            });
    }

    private static string CreateWebUserJwt(UserRole role)
    {
        // Create a locally signed test JWT that exercises the configured API authorization middleware.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey));
        var token = new JwtSecurityToken(
            issuer: "SolGrid.Tests",
            audience: "SolGrid.Tests",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "test-web-user"),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // No web user is seeded in this isolated host.
            return Task.FromResult<User?>(null);
        }
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // No web user is seeded in this isolated host.
            return Task.FromResult<User?>(null);
        }
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return the empty web user store.
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }
        public Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
        {
            // Return an empty page for this host.
            return Task.FromResult(new PagedResult<User> { Items = Array.Empty<User>(), TotalCount = 0, PageNumber = query.PageNumber, PageSize = query.PageSize });
        }
        public Task<bool> ExistsByEmailAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Registration must not reach a real MongoDB repository.
            return Task.FromResult(false);
        }
        public Task<bool> EmailExistsAsync(string email, string? excludingUserId = null, CancellationToken cancellationToken = default)
        {
            // Match the alternate repository query contract.
            return Task.FromResult(false);
        }
        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            // Fail loudly if this prosumer host unexpectedly writes a web user.
            throw new NotSupportedException();
        }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            // Fail loudly if this prosumer host unexpectedly writes a web user.
            throw new NotSupportedException();
        }
    }

    private sealed class TestProsumerRepository : IProsumerRepository
    {
        private readonly List<Prosumer> prosumers = [];

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
            // Return a simple page for future-contract compatibility in API tests.
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
            // Check NIC uniqueness in test storage.
            return Task.FromResult(prosumers.Any(prosumer => prosumer.Nic == nic.Trim().ToUpperInvariant()));
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            string? excludingNic = null,
            CancellationToken cancellationToken = default)
        {
            // Check email uniqueness in test storage.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(prosumers.Any(prosumer =>
                prosumer.Email == normalizedEmail && prosumer.Nic != excludingNic));
        }

        public Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Add a test prosumer to in-memory storage.
            prosumers.Add(prosumer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Preserve future repository-contract compatibility without update behavior.
            return Task.CompletedTask;
        }
    }
}
