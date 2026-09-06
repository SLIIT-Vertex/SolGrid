/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SlotsApiTests.cs
 * Description: Verifies booking slot API authorization and OpenAPI contracts.
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

public sealed class SlotsApiTests
{
    private const string TestJwtSigningKey = "test-signing-key-for-solgrid-auth-tests-32";
    private static readonly DateTimeOffset MondayStart = new(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OpenApiDocument_IncludesBookingSlotManagementEndpoints()
    {
        // Verify OpenAPI advertises the booking slot management contract.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var openApiJson = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"/api/v1/stations/{stationId}/slots\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/slots/{id}\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/slots/{id}/activate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/slots/{id}/deactivate\"", openApiJson, StringComparison.Ordinal);
        Assert.Contains("\"post\"", openApiJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"put\"", openApiJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"patch\"", openApiJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSlot_WithoutJwt_ReturnsUnauthorized()
    {
        // Verify slot creation requires authentication.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/stations/station-1/slots", CreateBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSlot_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot create booking slots.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.PostAsJsonAsync("/api/v1/stations/station-1/slots", CreateBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateSlot_WithGridOperatorJwt_ReturnsForbidden()
    {
        // Verify GridOperator claims cannot deactivate booking slots.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.PatchAsync("/api/v1/slots/slot-1/deactivate", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSlot_WithBackofficeJwt_ReturnsCreatedSlot()
    {
        // Verify Backoffice callers can create a valid slot through the public API.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.Backoffice));

        var response = await client.PostAsJsonAsync("/api/v1/stations/station-1/slots", CreateBody());
        var body = await response.Content.ReadFromJsonAsync<SlotBody>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("station-1", body!.StationId);
        Assert.Equal(SlotStatus.Available, body.Status);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/slots/{body.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetStationSlots_WithGridOperatorJwt_ReturnsOk()
    {
        // Verify GridOperator callers can list slots for operational use.
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateWebUserJwt(UserRole.GridOperator));

        var response = await client.GetAsync("/api/v1/stations/station-1/slots?pageNumber=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        // Create an API test host with in-memory station/slot persistence and JWT test configuration.
        var stationRepository = new TestSolarStationRepository(CreateStation());
        var slotRepository = new TestBookingSlotRepository();

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
                    services.AddSingleton<ISolarStationRepository>(stationRepository);
                    services.RemoveAll<IBookingSlotRepository>();
                    services.AddSingleton<IBookingSlotRepository>(slotRepository);
                    services.RemoveAll<IStationReservationLookup>();
                    services.AddSingleton<IStationReservationLookup>(new TestStationReservationLookup());
                    services.RemoveAll<ISolarStationService>();
                    services.AddScoped<ISolarStationService, SolarStationService>();
                    services.RemoveAll<IBookingSlotService>();
                    services.AddScoped<IBookingSlotService, BookingSlotService>();
                    services.RemoveAll<IReservationBookingSlotReadService>();
                    services.AddScoped<IReservationBookingSlotReadService, ReservationBookingSlotReadService>();
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
        // Build a valid slot create payload aligned to the test station schedule.
        return new
        {
            SlotNumber = 1,
            BatteryCapacityKwh = 12.5m,
            StartTime = MondayStart,
            EndTime = MondayStart.AddHours(2)
        };
    }

    private static SolarStation CreateStation()
    {
        // Create the station targeted by nested slot API tests.
        return SolarStation.Create(
            "station-1",
            "ST-1",
            "Colombo North",
            "Colombo",
            GeoCoordinates.Create(6.9271, 79.8612),
            50m,
            [],
            [OperatingWindow.Create(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            DateTimeOffset.UtcNow);
    }

    private sealed class SlotBody
    {
        public string Id { get; init; } = string.Empty;

        public string StationId { get; init; } = string.Empty;

        public SlotStatus Status { get; init; }
    }

    private sealed class TestStationReservationLookup : IStationReservationLookup
    {
        public Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default)
        {
            // Report no live station bookings unless a specific API test configures otherwise.
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
        private readonly List<SolarStation> stations;

        public TestSolarStationRepository(params SolarStation[] stations)
        {
            // Store test stations in memory.
            this.stations = stations.ToList();
        }

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
            SolarStationQuery query, CancellationToken cancellationToken = default)
        {
            // Return a simple page for unused station list calls.
            return Task.FromResult(new PagedResult<SolarStation>
            {
                Items = stations,
                TotalCount = stations.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<IReadOnlyList<SolarStation>> GetNearbyAsync(
            NearbyStationQuery query, CancellationToken cancellationToken = default)
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

    private sealed class TestBookingSlotRepository : IBookingSlotRepository
    {
        private readonly List<EnergyBookingSlot> slots = [];

        public Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a test slot by id.
            return Task.FromResult(slots.FirstOrDefault(slot => slot.Id == id));
        }

        public Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(
            BookingSlotQuery query, CancellationToken cancellationToken = default)
        {
            // Return a simple page for API list authorization tests.
            var items = slots
                .Where(slot => string.IsNullOrWhiteSpace(query.StationId) || slot.StationId == query.StationId)
                .ToArray();

            return Task.FromResult(new PagedResult<EnergyBookingSlot>
            {
                Items = items,
                TotalCount = items.Length,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Add a test slot to in-memory storage.
            slots.Add(slot);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Preserve repository-contract compatibility without extra update behavior.
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            // Remove a test slot from in-memory storage.
            slots.RemoveAll(slot => slot.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsBySlotNumberAsync(
            string stationId,
            int slotNumber,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Check slot-number uniqueness in test storage.
            return Task.FromResult(slots.Any(slot =>
                slot.StationId == stationId && slot.SlotNumber == slotNumber && slot.Id != excludingSlotId));
        }

        public Task<bool> HasOverlappingIntervalAsync(
            string stationId,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Detect overlapping test intervals for the same station.
            return Task.FromResult(slots.Any(slot =>
                slot.StationId == stationId
                && slot.Id != excludingSlotId
                && slot.StartTime < endTime
                && slot.EndTime > startTime));
        }
    }
}
