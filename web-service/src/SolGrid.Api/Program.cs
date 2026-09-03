using Microsoft.AspNetCore.Authentication.JwtBearer;
using SolGrid.Api.Middleware;
using SolGrid.Api.Security;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Services;
using SolGrid.Application.Common.Identity;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.DependencyInjection;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Configure API, application, infrastructure, authentication, and authorization services.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
    // Expose Program for API integration tests.
}
