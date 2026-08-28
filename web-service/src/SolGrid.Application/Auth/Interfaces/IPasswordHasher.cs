/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IPasswordHasher.cs
 * Description: Defines password hashing operations without exposing implementation details.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Auth.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
