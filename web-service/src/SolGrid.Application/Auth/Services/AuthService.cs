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
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Responses;

namespace SolGrid.Application.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IProsumerRepository? prosumerRepository;

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

    public AuthService(
        IUserRepository userRepository,
        IProsumerRepository prosumerRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
        : this(userRepository, passwordHasher, tokenService)
    {
        // Capture prosumer persistence for the shared authentication workflow.
        this.prosumerRepository = prosumerRepository;
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

    public async Task<ProsumerLoginResponse> LoginProsumerAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Authenticate an active prosumer through the shared password and token abstractions.
        ValidateRequest(request);
        var repository = prosumerRepository ?? throw new InvalidOperationException("Prosumer authentication is not configured.");
        var prosumer = await repository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);

        if (prosumer is null || !passwordHasher.VerifyPassword(request.Password, prosumer.PasswordHash))
        {
            throw new AuthenticationFailedException();
        }

        if (!prosumer.IsActive)
        {
            throw new AccountInactiveException();
        }

        var token = tokenService.CreateProsumerToken(prosumer.Nic);
        return new ProsumerLoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            Prosumer = ProsumerResponseMapper.ToResponse(prosumer)
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
