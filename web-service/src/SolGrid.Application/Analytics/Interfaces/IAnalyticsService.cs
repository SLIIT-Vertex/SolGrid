/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IAnalyticsService.cs
 * Description: Defines the operational analytics snapshot for web consoles.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Application.Analytics.Responses;

namespace SolGrid.Application.Analytics.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
