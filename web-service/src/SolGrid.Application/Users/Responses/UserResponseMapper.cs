/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserResponseMapper.cs
 * Description: Maps web user domain entities to safe response DTOs.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Entities;

namespace SolGrid.Application.Users.Responses;

public static class UserResponseMapper
{
    public static UserResponse ToResponse(User user)
    {
        // Map a user without exposing password hash details.
        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
