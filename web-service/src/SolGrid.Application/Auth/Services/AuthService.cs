/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthService.cs
 * Description: Handles web user authentication and token issuance.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Responses;

namespace SolGrid.Application.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        // Capture authentication dependencies.
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Validate credentials, enforce account status, and issue a signed access token.
        ValidateRequest(request);

        var user = await userRepository
            .GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException();
        }

        if (!user.IsActive)
        {
            throw new AccountInactiveException();
        }

        var token = tokenService.CreateToken(user);

        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = UserResponseMapper.ToResponse(user)
        };
    }

    private static void ValidateRequest(LoginRequest request)
    {
        // Ensure login requests contain required credential fields.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
