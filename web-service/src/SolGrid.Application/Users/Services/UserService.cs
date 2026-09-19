/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserService.cs
 * Description: Handles web user management business use cases.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Common.Validation;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Requests;
using SolGrid.Application.Users.Responses;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Users.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ICurrentUserContext currentUserContext;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserContext currentUserContext)
    {
        // Capture user management dependencies.
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.currentUserContext = currentUserContext;
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate, hash credentials, and persist a new web user.
        ValidateCreateRequest(request);
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("Email is already assigned to another user.");
        }

        var createdAt = DateTimeOffset.UtcNow;
        var user = User.Create(
            Guid.NewGuid().ToString("N"),
            request.FirstName,
            request.LastName,
            normalizedEmail,
            passwordHasher.HashPassword(request.Password),
            request.Role,
            createdAt);

        await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return UserResponseMapper.ToResponse(user);
    }

    public async Task<UserResponse> UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate and update only intended editable web user fields.
        ValidateId(id);
        ValidateUpdateRequest(request);

        var user = await GetRequiredUserAsync(id, cancellationToken).ConfigureAwait(false);
        var normalizedEmail = NormalizeEmail(request.Email);

        if (IsCurrentUser(id) && user.Role != request.Role)
        {
            throw new ForbiddenException("You cannot change your own role.");
        }

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            && await userRepository.ExistsByEmailAsync(normalizedEmail, user.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("Email is already assigned to another user.");
        }

        var updatedAt = DateTimeOffset.UtcNow;
        user.UpdateProfile(request.FirstName, request.LastName, normalizedEmail, updatedAt);
        user.ChangeRole(request.Role, updatedAt);

        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return UserResponseMapper.ToResponse(user);
    }

    public async Task ResetPasswordAsync(
        string id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate and replace a web user's password hash without ever persisting plaintext.
        ValidateId(id);
        ValidateResetPasswordRequest(request);

        var user = await GetRequiredUserAsync(id, cancellationToken).ConfigureAwait(false);
        var updatedAt = DateTimeOffset.UtcNow;
        user.ChangePasswordHash(passwordHasher.HashPassword(request.NewPassword), updatedAt);

        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserResponse> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Return one web user by id without exposing password hash details.
        ValidateId(id);
        var user = await GetRequiredUserAsync(id, cancellationToken).ConfigureAwait(false);
        return UserResponseMapper.ToResponse(user);
    }

    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        // Return all web users for administrative callers.
        var users = await userRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return users.Select(UserResponseMapper.ToResponse).ToArray();
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged web users for administrative screens.
        ValidateQuery(query);
        var users = await userRepository.GetPagedAsync(query, cancellationToken).ConfigureAwait(false);

        return new PagedResult<UserResponse>
        {
            Items = users.Items.Select(UserResponseMapper.ToResponse).ToArray(),
            TotalCount = users.TotalCount,
            PageNumber = users.PageNumber,
            PageSize = users.PageSize
        };
    }

    public Task ActivateUserAsync(string id, CancellationToken cancellationToken = default)
    {
        // Preserve the activation contract as an alias for reactivation.
        return ReactivateUserAsync(id, cancellationToken);
    }

    public async Task ReactivateUserAsync(string id, CancellationToken cancellationToken = default)
    {
        // Restore login eligibility by marking the account active.
        ValidateId(id);
        var user = await GetRequiredUserAsync(id, cancellationToken).ConfigureAwait(false);
        user.Activate(DateTimeOffset.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateUserAsync(string id, CancellationToken cancellationToken = default)
    {
        // Remove login eligibility by marking the account inactive.
        ValidateId(id);

        if (IsCurrentUser(id))
        {
            throw new ForbiddenException("You cannot deactivate your own account.");
        }

        var user = await GetRequiredUserAsync(id, cancellationToken).ConfigureAwait(false);
        user.Deactivate(DateTimeOffset.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<User> GetRequiredUserAsync(string id, CancellationToken cancellationToken)
    {
        // Load a user or report a client-safe not-found error.
        var user = await userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return user ?? throw new NotFoundException("User", id);
    }

    private static void ValidateCreateRequest(CreateUserRequest request)
    {
        // Validate required fields and supported roles for user creation.
        var errors = ValidateProfileFields(request.FirstName, request.LastName, request.Email, request.Role).ToList();

        if (!UserRequestValidationRules.HasValue(request.Password))
        {
            errors.Add("Password is required.");
        }
        else if (request.Password.Length < UserRequestValidationRules.PasswordMinimumLength)
        {
            errors.Add($"Password must be at least {UserRequestValidationRules.PasswordMinimumLength} characters.");
        }
        else if (request.Password.Length > UserRequestValidationRules.PasswordMaximumLength)
        {
            errors.Add($"Password must be no more than {UserRequestValidationRules.PasswordMaximumLength} characters.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateUpdateRequest(UpdateUserRequest request)
    {
        // Validate editable profile fields and supported role updates.
        ThrowIfInvalid(ValidateProfileFields(request.FirstName, request.LastName, request.Email, request.Role));
    }

    private static void ValidateResetPasswordRequest(ResetPasswordRequest request)
    {
        // Apply the same password strength rules used at account creation.
        var errors = new List<string>();

        if (!UserRequestValidationRules.HasValue(request.NewPassword))
        {
            errors.Add("Password is required.");
        }
        else if (request.NewPassword.Length < UserRequestValidationRules.PasswordMinimumLength)
        {
            errors.Add($"Password must be at least {UserRequestValidationRules.PasswordMinimumLength} characters.");
        }
        else if (request.NewPassword.Length > UserRequestValidationRules.PasswordMaximumLength)
        {
            errors.Add($"Password must be no more than {UserRequestValidationRules.PasswordMaximumLength} characters.");
        }

        ThrowIfInvalid(errors);
    }

    private static IEnumerable<string> ValidateProfileFields(
        string firstName,
        string lastName,
        string email,
        UserRole role)
    {
        // Validate shared profile fields for create and update workflows.
        if (!UserRequestValidationRules.HasValue(firstName))
        {
            yield return "First name is required.";
        }
        else if (firstName.Trim().Length > UserRequestValidationRules.NameMaximumLength)
        {
            yield return $"First name must be no more than {UserRequestValidationRules.NameMaximumLength} characters.";
        }

        if (!UserRequestValidationRules.HasValue(lastName))
        {
            yield return "Last name is required.";
        }
        else if (lastName.Trim().Length > UserRequestValidationRules.NameMaximumLength)
        {
            yield return $"Last name must be no more than {UserRequestValidationRules.NameMaximumLength} characters.";
        }

        if (!UserRequestValidationRules.HasEmailShape(email))
        {
            yield return "A valid email is required.";
        }

        if (!Enum.IsDefined(role))
        {
            yield return "User role is not supported.";
        }
    }

    private static void ValidateId(string id)
    {
        // Validate that route ids contain meaningful text.
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException(["User id is required."]);
        }
    }

    private static void ValidateQuery(UserQuery query)
    {
        // Validate query filters before passing them to persistence.
        var errors = new List<string>();

        if (query.Role.HasValue && !Enum.IsDefined(query.Role.Value))
        {
            errors.Add("User role filter is not supported.");
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            errors.Add("Account status filter is not supported.");
        }

        if (query.PageNumber < 1)
        {
            errors.Add("Page number must be greater than zero.");
        }

        if (query.PageSize < 1)
        {
            errors.Add("Page size must be greater than zero.");
        }

        if (query.PageSize > UserRequestValidationRules.PageSizeMaximum)
        {
            errors.Add($"Page size must be no more than {UserRequestValidationRules.PageSizeMaximum}.");
        }

        if (query.SearchText?.Trim().Length > UserRequestValidationRules.SearchTextMaximumLength)
        {
            errors.Add($"Search text must be no more than {UserRequestValidationRules.SearchTextMaximumLength} characters.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ThrowIfInvalid(IEnumerable<string> errors)
    {
        // Throw one validation exception containing all collected validation messages.
        var errorList = errors.ToArray();
        if (errorList.Length > 0)
        {
            throw new ValidationException(errorList);
        }
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email consistently for uniqueness and authentication.
        return email.Trim().ToLowerInvariant();
    }

    private bool IsCurrentUser(string id)
    {
        // Prevent administrators from accidentally locking themselves out of user management.
        return currentUserContext.IsAuthenticated
            && string.Equals(currentUserContext.UserId, id, StringComparison.Ordinal);
    }
}
