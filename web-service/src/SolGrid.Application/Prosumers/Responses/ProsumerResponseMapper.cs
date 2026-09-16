/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerResponseMapper.cs
 * Description: Maps prosumer domain entities to API-safe response contracts.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Entities;

namespace SolGrid.Application.Prosumers.Responses;

public static class ProsumerResponseMapper
{
    public static ProsumerResponse ToResponse(Prosumer prosumer)
    {
        // Map a prosumer profile without exposing persistence or future credential fields.
        return new ProsumerResponse
        {
            Nic = prosumer.Nic,
            FirstName = prosumer.FirstName,
            LastName = prosumer.LastName,
            Email = prosumer.Email,
            PhoneNumber = prosumer.PhoneNumber,
            Status = prosumer.Status,
            CreatedAt = prosumer.CreatedAt,
            UpdatedAt = prosumer.UpdatedAt
        };
    }
}
