/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationExpiryWorker.cs
 * Description: Runs periodic server-side expiry processing for overdue reservations.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Services;

namespace SolGrid.Api.BackgroundServices;

public sealed class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ReservationExpiryWorker> logger;

    public ReservationExpiryWorker(IServiceScopeFactory serviceScopeFactory, ILogger<ReservationExpiryWorker> logger)
    {
        // Capture scoped service creation and logging dependencies for recurring background processing.
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Repair legacy committed slots once, process expiry immediately, then repeat at the configured interval.
        await ReopenExpiredBookingSlotsAsync(stoppingToken).ConfigureAwait(false);
        await ExpireDueReservationsAsync(stoppingToken).ConfigureAwait(false);
        using var timer = new PeriodicTimer(ReservationExpiryPolicy.WorkerInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExpireDueReservationsAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ReopenExpiredBookingSlotsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var expiryService = scope.ServiceProvider.GetRequiredService<IReservationExpiryService>();
            var slotCount = await expiryService.ReopenExpiredBookingSlotsAsync(stoppingToken).ConfigureAwait(false);
            if (slotCount > 0)
            {
                logger.LogInformation("Reconciled {BookingSlotCount} slots linked to expired reservations.", slotCount);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Allow normal host shutdown without reporting a false worker failure.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Expired reservation slot reconciliation failed.");
        }
    }

    private async Task ExpireDueReservationsAsync(CancellationToken stoppingToken)
    {
        // Keep the worker alive after a transient failure while preserving shutdown cancellation semantics.
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var expiryService = scope.ServiceProvider.GetRequiredService<IReservationExpiryService>();
            var expiredCount = await expiryService.ExpireDueReservationsAsync(stoppingToken).ConfigureAwait(false);
            if (expiredCount > 0)
            {
                logger.LogInformation("Expired {ReservationCount} overdue reservations.", expiredCount);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Allow normal host shutdown without reporting a false worker failure.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reservation expiry processing failed.");
        }
    }
}
