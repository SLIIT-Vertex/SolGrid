/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerService.cs
 * Description: Handles prosumer registration business rules.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Validation;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Prosumers.Validation;
using SolGrid.Domain.Entities;

namespace SolGrid.Application.Prosumers.Services;

public sealed class ProsumerService : IProsumerService
{
    private readonly IProsumerRepository prosumerRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly TimeProvider timeProvider;

    public ProsumerService(
        IProsumerRepository prosumerRepository,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        // Capture registration dependencies without coupling to infrastructure implementations.
        this.prosumerRepository = prosumerRepository;
        this.passwordHasher = passwordHasher;
        this.timeProvider = timeProvider;
    }

    public async Task<ProsumerResponse> RegisterProsumerAsync(
        RegisterProsumerRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate, normalize, hash credentials, and persist a pending prosumer registration.
        ValidateRegistrationRequest(request);

        var nic = NormalizeNic(request.Nic);
        var email = NormalizeEmail(request.Email);

        if (await prosumerRepository.ExistsByNicAsync(nic, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("NIC is already registered to another prosumer.");
        }

        if (await prosumerRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("Email is already registered to another prosumer.");
        }

        var createdAt = timeProvider.GetUtcNow();
        var prosumer = Prosumer.Create(
            nic,
            request.FirstName,
            request.LastName,
            email,
            request.PhoneNumber,
            passwordHasher.HashPassword(request.Password),
            createdAt);

        await prosumerRepository.AddAsync(prosumer, cancellationToken).ConfigureAwait(false);
        return ProsumerResponseMapper.ToResponse(prosumer);
    }

    private static void ValidateRegistrationRequest(RegisterProsumerRequest request)
    {
        // Validate required registration fields before repository or password-hashing work begins.
        var errors = new List<string>();

        if (!ProsumerRequestValidationRules.HasSupportedSriLankanNicFormat(request.Nic))
        {
            errors.Add("A valid Sri Lankan NIC is required.");
        }

        if (!UserRequestValidationRules.HasValue(request.FirstName))
        {
            errors.Add("First name is required.");
        }

        if (!UserRequestValidationRules.HasValue(request.LastName))
        {
            errors.Add("Last name is required.");
        }

        if (!ProsumerRequestValidationRules.HasEmailShape(request.Email))
        {
            errors.Add("A valid email is required.");
        }

        if (!ProsumerRequestValidationRules.HasPhoneNumberShape(request.PhoneNumber))
        {
            errors.Add("Phone number is not valid.");
        }

        if (!UserRequestValidationRules.HasValue(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static string NormalizeNic(string nic)
    {
        // Normalize NIC consistently for uniqueness checks and persistence.
        return nic.Trim().ToUpperInvariant();
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email consistently for uniqueness checks and persistence.
        return email.Trim().ToLowerInvariant();
    }
}
