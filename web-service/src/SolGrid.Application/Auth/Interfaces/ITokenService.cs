/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ITokenService.cs
 * Description: Defines token issuance for authenticated web users without JWT implementation coupling.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Auth.Responses;
using SolGrid.Domain.Entities;

namespace SolGrid.Application.Auth.Interfaces;

public interface ITokenService
{
    IssuedToken CreateToken(User user);
}
