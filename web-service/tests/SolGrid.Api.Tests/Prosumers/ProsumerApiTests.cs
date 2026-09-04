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
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SolGrid.Api.Tests.Prosumers;

public sealed class ProsumerApiTests
{
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
            PhoneNumber = "+94712345678",
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
                        ["Jwt:SigningKey"] = "test-signing-key-for-solgrid-auth-tests-32",
                        ["Jwt:ExpiresMinutes"] = "30",
                        ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:1",
                        ["MongoDb:DatabaseName"] = "SolGridTests",
                        ["MongoDb:InitializeOnStartup"] = "false"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IReservationService>();
                    services.RemoveAll<IProsumerRepository>();
                    services.AddSingleton<IProsumerRepository>(new TestProsumerRepository());
                });
            });
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
