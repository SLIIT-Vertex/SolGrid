/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: JwtTokenService.cs
 * Description: Issues signed JWT access tokens for authenticated web users.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Responses;
using SolGrid.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SolGrid.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        // Capture JWT issuing options from typed configuration.
        this.options = options.Value;
    }

    public IssuedToken CreateToken(User user)
    {
        // Issue a signed JWT containing only stable identity and role claims.
        ValidateOptions();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiresMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }

    private void ValidateOptions()
    {
        // Ensure JWT tokens cannot be issued with missing or weak configuration.
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey) || Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        if (options.ExpiresMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiry must be positive.");
        }
    }
}
