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
using SolGrid.Domain.ValueObjects;
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
        Assert.Contains("\"/api/v1/stations/nearby\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}/schedule\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}/activate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/stations/{id}/deactivate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"latitude\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"longitude\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"radiusKilometers\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"maxResults\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"activeOnly\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"searchText\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"hasAvailableSlots\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"pageNumber\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"pageSize\"", openApiJson, StringComparison.Ordinal);
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

    [Fact]
    public async Task GetNearbyStations_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify Android Maps discovery requires an authenticated caller.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stations/nearby?latitude=6.9271&longitude=79.8612");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNearbyStations_WithProsumerJwt_ReturnsStationsWithCoordinates()
    {
        // Verify Android receives live station coordinates from the API rather than embedded node data.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.Backoffice));
        await client.PostAsJsonAsync("/api/v1/stations", CreateBody());

        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateProsumerJwt());
        var response = await client.GetAsync(
            "/api/v1/stations/nearby?latitude=6.9271&longitude=79.8612&radiusKilometers=10&maxResults=20&activeOnly=true");
        var body = await response.Content.ReadFromJsonAsync<SolarStationBody[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var station = Assert.Single(body!);
        Assert.Equal("ST-1", station.Code);
        Assert.Equal(6.9271, station.Location.Latitude);
        Assert.Equal(79.8612, station.Location.Longitude);
        Assert.Equal(StationStatus.Active, station.Status);
        Assert.NotNull(station.DistanceKilometers);
    }

    [Fact]
    public async Task GetNearbyStations_WithInvalidLatitude_ReturnsBadRequest()
    {
        // Verify Maps queries reject coordinates that cannot describe a GPS origin.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateProsumerJwt());

        var response = await client.GetAsync("/api/v1/stations/nearby?latitude=91&longitude=79.8612");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStationById_WithProsumerJwt_ReturnsStation()
    {
        // Verify Android can load one station's live details after selecting a map marker.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.Backoffice));
        var created = await (await client.PostAsJsonAsync("/api/v1/stations", CreateBody()))
            .Content.ReadFromJsonAsync<SolarStationBody>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateProsumerJwt());
        var response = await client.GetAsync($"/api/v1/stations/{created!.Id}");
        var body = await response.Content.ReadFromJsonAsync<SolarStationBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body!.Id);
        Assert.Equal(6.9271, body.Location.Latitude);
    }

    [Fact]
    public async Task GetStations_WithProsumerJwt_ReturnsForbidden()
    {
        // Verify administrative station lists stay closed to Android prosumer tokens.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateProsumerJwt());

        var response = await client.GetAsync("/api/v1/stations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceSchedule_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot replace a station operating schedule.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.PutAsJsonAsync("/api/v1/stations/station-1/schedule", new
        {
            Schedule = new[]
            {
                new { Day = DayOfWeek.Monday, OpensAt = "09:00:00", ClosesAt = "16:00:00" }
            }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateStation_WithBackofficeJwt_ReturnsNoContent()
    {
        // Verify Backoffice can deactivate a station that has no live reservations.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.Backoffice));
        var created = await (await client.PostAsJsonAsync("/api/v1/stations", CreateBody()))
            .Content.ReadFromJsonAsync<SolarStationBody>();

        var response = await client.PatchAsync($"/api/v1/stations/{created!.Id}/deactivate", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
                    services.RemoveAll<IBookingSlotRepository>();
                    services.AddSingleton<IBookingSlotRepository>(new TestBookingSlotRepository());
                    services.RemoveAll<IStationReservationLookup>();
                    services.AddSingleton<IStationReservationLookup>(new TestStationReservationLookup());
                    services.RemoveAll<ISolarStationService>();
                    services.AddScoped<ISolarStationService, SolarStationService>();
                    services.RemoveAll<IBookingSlotService>();
                    services.AddScoped<IBookingSlotService, BookingSlotService>();
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

    private static string CreateProsumerJwt()
    {
        // Create a prosumer JWT that has a subject but no web-user role claim.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey));
        var token = new JwtSecurityToken(
            issuer: "SolGrid.Tests",
            audience: "SolGrid.Tests",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "199012345678")
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
        public string Id { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public StationStatus Status { get; init; }

        public LocationBody Location { get; init; } = new();

        public double? DistanceKilometers { get; init; }
    }

    private sealed class LocationBody
    {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    private sealed class TestStationReservationLookup : IStationReservationLookup
    {
        public Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default)
        {
            // Report no live bookings unless a specific API test configures otherwise.
            return Task.FromResult(0);
        }

        public Task<bool> HasActiveReservationForSlotAsync(
            string bookingSlotId, CancellationToken cancellationToken = default)
        {
            // Report no live slot bookings unless a specific API test configures otherwise.
            return Task.FromResult(false);
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
            // Rank in-memory stations by great-circle distance for Maps API tests.
            var origin = GeoCoordinates.Create(query.Latitude, query.Longitude);
            var stationsInRange = stations
                .Where(station => !query.ActiveOnly || station.IsActive)
                .Where(station => station.DistanceInKilometersFrom(origin) <= query.RadiusKilometers)
                .OrderBy(station => station.DistanceInKilometersFrom(origin))
                .Take(query.MaxResults)
                .ToArray();

            return Task.FromResult<IReadOnlyList<SolarStation>>(stationsInRange);
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
            // Replace the in-memory station so deactivation API tests observe the persisted change.
            var index = stations.FindIndex(existing => existing.Id == station.Id);
            if (index >= 0)
            {
                stations[index] = station;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestBookingSlotRepository : IBookingSlotRepository
    {
        public Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Station API tests do not look up independently persisted slots.
            return Task.FromResult<EnergyBookingSlot?>(null);
        }

        public Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(
            BookingSlotQuery query, CancellationToken cancellationToken = default)
        {
            // Station API tests do not page independently persisted slots.
            return Task.FromResult(new PagedResult<EnergyBookingSlot>
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Accept station-create slot persistence without extra assertions.
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Station API tests do not update independently persisted slots.
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            // Station API tests do not delete independently persisted slots.
            return Task.CompletedTask;
        }

        public Task<bool> ExistsBySlotNumberAsync(
            string stationId,
            int slotNumber,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Station API tests do not check independent slot-number uniqueness.
            return Task.FromResult(false);
        }

        public Task<bool> HasOverlappingIntervalAsync(
            string stationId,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Station API tests do not check independent slot interval overlap.
            return Task.FromResult(false);
        }
    }
}
