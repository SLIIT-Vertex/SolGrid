/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AnalyticsController.cs
 * Description: Exposes the operational analytics snapshot to web consoles.
 * Contributor: Dilshan Yapa
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Analytics.Interfaces;
using SolGrid.Application.Analytics.Responses;

namespace SolGrid.Api.Controllers;

[ApiController]
[Authorize(Roles = "Backoffice,GridOperator")]
[Route("api/v1/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        // Capture the application service used by the analytics endpoint.
        this.analyticsService = analyticsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsSnapshotResponse>> GetSnapshot(CancellationToken cancellationToken)
    {
        // Return node, reservation, and business-rule analytics calculated in the service.
        var response = await analyticsService.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
