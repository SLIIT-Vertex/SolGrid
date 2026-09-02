/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationQrTokenService.cs
 * Description: Defines secure QR transaction token operations for reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationQrTokenService
{
    string GenerateToken();

    string HashToken(string token);

    bool VerifyToken(string token, string tokenHash);
}
