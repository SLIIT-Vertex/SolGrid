/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: StationsApiTests.cs
 * Description: Verifies solar station API authorization and OpenAPI contracts.
 * Contributor: Kavishi Godage
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace SolGrid.Api.Tests.SolarStations;

public sealed class StationsApiTests
{
    private const string TestJwtSigningKey = "test-signing-key-for-solgrid-auth-tests-32";

    [Fact]
    public async Task OpenApiDocument_IncludesStationManagementEndpoints()
    {
        // Verify OpenAPI advertises the station management contract.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var openApiJson = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"/api/v1/stations\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}/activate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}/deactivate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"post\"", openApiJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"put\"", openApiJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"patch\"", openApiJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateStation_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify station creation requires authentication.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/stations", CreateBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateStation_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot create stations.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.PostAsJsonAsync("/api/v1/stations", CreateBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateStation_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot deactivate stations.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.PatchAsync("/api/v1/stations/station-1/deactivate", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateStation_WithBackofficeJwt_ReturnsCreatedStation()
    {
        // Verify Backoffice callers can create a valid station through the public API.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.Backoffice));

        var response = await client.PostAsJsonAsync("/api/v1/stations", CreateBody());
        var body = await response.Content.ReadFromJsonAsync<SolarStationBody>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("ST-1", body!.Code);
        Assert.Equal(StationStatus.Active, body.Status);
    }

    [Fact]
    public async Task GetStations_WithGridOperatorJwt_ReturnsOk()
    {
        // Verify GridOperator callers can list stations for operational use.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.GetAsync("/api/v1/stations?status=1&searchText=north&pageNumber=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        // Create an API test host with in-memory station persistence and JWT test configuration.
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
                    services.RemoveAll<ISolarStationRepository>();
                    services.AddSingleton<ISolarStationRepository>(new TestSolarStationRepository());
                    services.RemoveAll<IStationReservationLookup>();
                    services.AddSingleton<IStationReservationLookup>(new TestStationReservationLookup());
                    services.RemoveAll<ISolarStationService>();
                    services.AddScoped<ISolarStationService, SolarStationService>();
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

    private static object CreateBody()
    {
        // Build a valid station create payload for API tests.
        return new
        {
            Code = "ST-1",
            Name = "Colombo North",
            AddressLine = "Colombo",
            Location = new { Latitude = 6.9271, Longitude = 79.8612 },
            CapacityKw = 50,
            Slots = Array.Empty<object>(),
            Schedule = new[]
            {
                new
                {
                    Day = DayOfWeek.Monday,
                    OpensAt = "08:00:00",
                    ClosesAt = "17:00:00"
                }
            }
        };
    }

    private sealed class SolarStationBody
    {
        public string Code { get; init; } = string.Empty;

        public StationStatus Status { get; init; }
    }

    private sealed class TestStationReservationLookup : IStationReservationLookup
    {
        public Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default)
        {
            // Report no live bookings unless a specific API test configures otherwise.
            return Task.FromResult(0);
        }
    }

    private sealed class TestSolarStationRepository : ISolarStationRepository
    {
        private readonly List<SolarStation> stations = [];

        public Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a test station by id.
            return Task.FromResult(stations.FirstOrDefault(station => station.Id == id));
        }

        public Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            // Find a test station by normalized code.
            var normalizedCode = code.Trim().ToUpperInvariant();
            return Task.FromResult(stations.FirstOrDefault(station => station.Code == normalizedCode));
        }

        public Task<IReadOnlyList<SolarStation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return all test stations.
            return Task.FromResult<IReadOnlyList<SolarStation>>(stations);
        }

        public Task<PagedResult<SolarStation>> GetPagedAsync(
            SolarStationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Return a simple page for API list authorization tests.
            return Task.FromResult(new PagedResult<SolarStation>
            {
                Items = stations,
                TotalCount = stations.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<IReadOnlyList<SolarStation>> GetNearbyAsync(
            NearbyStationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Nearby discovery is not exercised by these API authorization tests.
            return Task.FromResult<IReadOnlyList<SolarStation>>(stations);
        }

        public Task<bool> ExistsByCodeAsync(
            string code,
            string? excludingStationId = null,
            CancellationToken cancellationToken = default)
        {
            // Check code uniqueness in test storage.
            var normalizedCode = code.Trim().ToUpperInvariant();
            return Task.FromResult(stations.Any(station =>
                station.Code == normalizedCode && station.Id != excludingStationId));
        }

        public Task AddAsync(SolarStation station, CancellationToken cancellationToken = default)
        {
            // Add a test station to in-memory storage.
            stations.Add(station);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SolarStation station, CancellationToken cancellationToken = default)
        {
            // Preserve repository-contract compatibility without extra update behavior.
            return Task.CompletedTask;
        }
    }
}
