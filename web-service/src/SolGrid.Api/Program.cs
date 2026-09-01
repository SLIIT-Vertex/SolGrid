/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: Program.cs
 * Description: Configures the SolGrid Web API host, authentication, authorization, and dependency injection.
 * Contributor: Dilshan Yapa
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using SolGrid.Api.Middleware;
using SolGrid.Api.Security;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Services;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Services;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.DependencyInjection;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Seeding;
using SolGrid.Infrastructure.Security;

const string CorsPolicyName = "SolGridClientCors";

var builder = WebApplication.CreateBuilder(args);

// Configure API, application, infrastructure, authentication, and authorization services.
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    // Allow configured browser clients to call the API without allowing credentials from wildcard origins.
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSolGridInfrastructure(
    mongoDbOptions => builder.Configuration.GetSection("MongoDb").Bind(mongoDbOptions),
    jwtOptions => builder.Configuration.GetSection("Jwt").Bind(jwtOptions));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();

builder.Services.AddAuthorization(options =>
{
    // Register server-side role policies for web user authorization.
    options.AddPolicy(AuthorizationPolicies.Backoffice, policy => policy.RequireRole(UserRole.Backoffice.ToString()));
    options.AddPolicy(AuthorizationPolicies.GridOperator, policy => policy.RequireRole(UserRole.GridOperator.ToString()));
});
builder.Services.AddOpenApi(options =>
{
    // Add JWT bearer authentication metadata to generated OpenAPI output.
    options.AddDocumentTransformer<BearerSecurityOpenApiTransformer>();
});

var app = builder.Build();

// Configure middleware order for errors, authentication, authorization, and controllers.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Initialize MongoDB indexes and optional development seed users before serving traffic.
using (var startupScope = app.Services.CreateScope())
{
    var mongoDbOptions = startupScope.ServiceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    if (mongoDbOptions.InitializeOnStartup)
    {
        var userCollectionInitializer = startupScope.ServiceProvider.GetRequiredService<IUserCollectionInitializer>();
        await userCollectionInitializer.EnsureCreatedAsync().ConfigureAwait(false);

        var reservationCollectionInitializer = startupScope.ServiceProvider.GetRequiredService<IReservationCollectionInitializer>();
        await reservationCollectionInitializer.EnsureCreatedAsync().ConfigureAwait(false);

        var userSeedDataInitializer = startupScope.ServiceProvider.GetRequiredService<IUserSeedDataInitializer>();
        await userSeedDataInitializer.SeedAsync().ConfigureAwait(false);
    }
}

app.Run();

public partial class Program
{
    // Expose Program for API integration tests.
}
