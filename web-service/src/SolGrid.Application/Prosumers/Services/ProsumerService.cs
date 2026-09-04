/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerService.cs
 * Description: Handles prosumer registration business rules.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Validation;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Prosumers.Validation;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Prosumers.Services;

public sealed class ProsumerService : IProsumerService
{
    private readonly IProsumerRepository prosumerRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly TimeProvider timeProvider;
    private readonly ICurrentUserContext? currentUserContext;

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

    public ProsumerService(
        IProsumerRepository prosumerRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
        : this(prosumerRepository, passwordHasher, timeProvider)
    {
        // Capture trusted current-user identity for profile ownership and Backoffice checks.
        this.currentUserContext = currentUserContext;
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

    public async Task<ProsumerResponse> GetMyProsumerAsync(CancellationToken cancellationToken = default)
    {
        // Return the profile identified solely by the authenticated prosumer token subject.
        var prosumer = await GetCurrentProsumerAsync(cancellationToken).ConfigureAwait(false);
        EnsureSelfServiceEligible(prosumer);
        return ProsumerResponseMapper.ToResponse(prosumer);
    }

    public async Task<ProsumerResponse> UpdateMyProsumerAsync(UpdateProsumerRequest request, CancellationToken cancellationToken = default)
    {
        // Update only the authenticated prosumer's editable profile fields.
        ValidateUpdateRequest(request);
        var prosumer = await GetCurrentProsumerAsync(cancellationToken).ConfigureAwait(false);
        EnsureSelfServiceEligible(prosumer);
        var email = NormalizeEmail(request.Email);

        if (!string.Equals(prosumer.Email, email, StringComparison.OrdinalIgnoreCase)
            && await prosumerRepository.ExistsByEmailAsync(email, prosumer.Nic, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("Email is already registered to another prosumer.");
        }

        prosumer.UpdateProfile(request.FirstName, request.LastName, email, request.PhoneNumber, timeProvider.GetUtcNow());
        await prosumerRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
        return ProsumerResponseMapper.ToResponse(prosumer);
    }

    public async Task RequestMyDeactivationAsync(CancellationToken cancellationToken = default)
    {
        // Record an authenticated active prosumer's account-deactivation request.
        var prosumer = await GetCurrentProsumerAsync(cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => prosumer.RequestDeactivation(timeProvider.GetUtcNow()));
        await prosumerRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<ProsumerResponse>> GetProsumersAsync(ProsumerQuery query, CancellationToken cancellationToken = default)
    {
        // Return a filtered Backoffice prosumer management page.
        EnsureBackoffice();
        ValidateQuery(query);
        var prosumers = await prosumerRepository.GetPagedAsync(query, cancellationToken).ConfigureAwait(false);
        return new PagedResult<ProsumerResponse>
        {
            Items = prosumers.Items.Select(ProsumerResponseMapper.ToResponse).ToArray(),
            TotalCount = prosumers.TotalCount,
            PageNumber = prosumers.PageNumber,
            PageSize = prosumers.PageSize
        };
    }

    public async Task<ProsumerResponse> GetProsumerByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Return one prosumer profile to an authorized Backoffice caller.
        EnsureBackoffice();
        var prosumer = await GetRequiredProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        return ProsumerResponseMapper.ToResponse(prosumer);
    }

    public async Task ActivateProsumerAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Activate a pending prosumer through a Backoffice-only lifecycle transition.
        EnsureBackoffice();
        var prosumer = await GetRequiredProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => prosumer.Activate(timeProvider.GetUtcNow()));
        await prosumerRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateProsumerAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Deactivate a prosumer through a Backoffice-only lifecycle transition.
        EnsureBackoffice();
        var prosumer = await GetRequiredProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => prosumer.Deactivate(timeProvider.GetUtcNow()));
        await prosumerRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReactivateProsumerAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Reactivate a deactivated prosumer through a Backoffice-only lifecycle transition.
        EnsureBackoffice();
        var prosumer = await GetRequiredProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        ApplyLifecycleTransition(() => prosumer.Reactivate(timeProvider.GetUtcNow()));
        await prosumerRepository.UpdateAsync(prosumer, cancellationToken).ConfigureAwait(false);
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

    private static void ValidateUpdateRequest(UpdateProsumerRequest request)
    {
        // Validate the explicit editable fields accepted by own-profile updates.
        var errors = new List<string>();
        if (!UserRequestValidationRules.HasValue(request.FirstName)) errors.Add("First name is required.");
        if (!UserRequestValidationRules.HasValue(request.LastName)) errors.Add("Last name is required.");
        if (!ProsumerRequestValidationRules.HasEmailShape(request.Email)) errors.Add("A valid email is required.");
        if (!ProsumerRequestValidationRules.HasPhoneNumberShape(request.PhoneNumber)) errors.Add("Phone number is not valid.");
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static void ValidateQuery(ProsumerQuery query)
    {
        // Validate Backoffice list filters before passing them to persistence.
        var errors = new List<string>();
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value)) errors.Add("Prosumer status filter is not supported.");
        if (query.PageNumber < 1) errors.Add("Page number must be greater than zero.");
        if (query.PageSize < 1) errors.Add("Page size must be greater than zero.");
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private async Task<Prosumer> GetCurrentProsumerAsync(CancellationToken cancellationToken)
    {
        // Resolve an active authenticated prosumer only from the trusted JWT subject claim.
        if (currentUserContext is null || !currentUserContext.IsAuthenticated
            || currentUserContext.Role is not null || string.IsNullOrWhiteSpace(currentUserContext.UserId))
        {
            throw new ForbiddenException("Current prosumer identity is required.");
        }

        return await GetRequiredProsumerAsync(currentUserContext.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Prosumer> GetRequiredProsumerAsync(string nic, CancellationToken cancellationToken)
    {
        // Load a prosumer or return a client-safe not-found error.
        var prosumer = await prosumerRepository.GetByNicAsync(NormalizeNic(nic), cancellationToken).ConfigureAwait(false);
        return prosumer ?? throw new NotFoundException("Prosumer", NormalizeNic(nic));
    }

    private void EnsureBackoffice()
    {
        // Restrict administrative prosumer lifecycle operations to Backoffice users.
        if (currentUserContext?.Role != UserRole.Backoffice)
        {
            throw new ForbiddenException("Backoffice authorization is required for prosumer administration.");
        }
    }

    private static void EnsureSelfServiceEligible(Prosumer prosumer)
    {
        // Block stale-token self-service access after a prosumer leaves the active lifecycle state.
        if (!prosumer.IsActive)
        {
            throw new AccountInactiveException();
        }
    }

    private static void ApplyLifecycleTransition(Action transition)
    {
        // Translate domain lifecycle violations into API conflict behavior.
        try { transition(); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
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
