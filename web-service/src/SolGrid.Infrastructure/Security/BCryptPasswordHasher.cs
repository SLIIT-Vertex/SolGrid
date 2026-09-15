/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BCryptPasswordHasher.cs
 * Description: Hashes and verifies passwords using BCrypt.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Interfaces;

namespace SolGrid.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // Hash a plaintext password using BCrypt before persistence.
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        // Verify a plaintext password against a stored BCrypt hash.
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
