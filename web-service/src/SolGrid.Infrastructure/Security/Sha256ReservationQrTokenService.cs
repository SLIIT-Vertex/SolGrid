/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: Sha256ReservationQrTokenService.cs
 * Description: Creates and verifies secure random QR transaction tokens for reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Reservations.Interfaces;
using System.Security.Cryptography;

namespace SolGrid.Infrastructure.Security;

public sealed class Sha256ReservationQrTokenService : IReservationQrTokenService
{
    private const int TokenByteLength = 32;

    public string GenerateToken()
    {
        // Generate a cryptographically random URL-safe token for QR payloads.
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
    }

    public string HashToken(string token)
    {
        // Hash the raw QR token before persistence.
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyToken(string token, string tokenHash)
    {
        // Compare token hashes in fixed time to avoid leaking comparison details.
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(tokenHash))
        {
            return false;
        }

        try
        {
            var computedHash = Convert.FromBase64String(HashToken(token));
            var expectedHash = Convert.FromBase64String(tokenHash);
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        // Convert random bytes into a compact URL-safe token string.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
