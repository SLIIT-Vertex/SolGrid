/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AnalyticsService.cs
 * Description: Loads stations, reservations, and accounts, then evaluates analytics in the service.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Application.Analytics.Interfaces;
using SolGrid.Application.Analytics.Responses;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Analytics.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private const int PageSize = 100;
    private const int MaximumPages = 1000;

    private readonly ISolarStationRepository stationRepository;
    private readonly IReservationRepository reservationRepository;
    private readonly IUserRepository userRepository;
    private readonly IProsumerRepository prosumerRepository;
    private readonly ICurrentUserContext currentUserContext;
    private readonly TimeProvider timeProvider;

    public AnalyticsService(
        ISolarStationRepository stationRepository,
        IReservationRepository reservationRepository,
        IUserRepository userRepository,
        IProsumerRepository prosumerRepository,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        // Capture the repositories and clock used to build one analytics snapshot.
        this.stationRepository = stationRepository;
        this.reservationRepository = reservationRepository;
        this.userRepository = userRepository;
        this.prosumerRepository = prosumerRepository;
        this.currentUserContext = currentUserContext;
        this.timeProvider = timeProvider;
    }

    public async Task<AnalyticsSnapshotResponse> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Allow only Backoffice and Grid Operator consoles to read operational analytics.
        if (currentUserContext.Role is not (UserRole.Backoffice or UserRole.GridOperator))
        {
            throw new ForbiddenException("Backoffice or GridOperator authorization is required to view analytics.");
        }

        var stationsTask = stationRepository.GetAllAsync(cancellationToken);
        var reservationsTask = LoadReservationsAsync(cancellationToken);
        await Task.WhenAll(stationsTask, reservationsTask).ConfigureAwait(false);

        var includeAccounts = currentUserContext.Role == UserRole.Backoffice;
        IReadOnlyList<User>? users = null;
        IReadOnlyList<Prosumer>? prosumers = null;
        if (includeAccounts)
        {
            users = await userRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            prosumers = await LoadProsumersAsync(cancellationToken).ConfigureAwait(false);
        }

        return AnalyticsComposer.Compose(
            stationsTask.Result,
            reservationsTask.Result,
            users,
            prosumers,
            timeProvider.GetUtcNow(),
            includeAccounts);
    }

    private async Task<IReadOnlyList<EnergyReservation>> LoadReservationsAsync(CancellationToken cancellationToken)
    {
        // Page through every reservation so totals are not cut off at one page.
        return await LoadPagesAsync(
            (pageNumber, token) => reservationRepository.GetPagedAsync(
                new ReservationQuery { PageNumber = pageNumber, PageSize = PageSize },
                token),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Prosumer>> LoadProsumersAsync(CancellationToken cancellationToken)
    {
        // Page through every prosumer so account analytics include the full register.
        return await LoadPagesAsync(
            (pageNumber, token) => prosumerRepository.GetPagedAsync(
                new ProsumerQuery { PageNumber = pageNumber, PageSize = PageSize },
                token),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<T>> LoadPagesAsync<T>(
        Func<int, CancellationToken, Task<PagedResult<T>>> loadPage,
        CancellationToken cancellationToken)
    {
        // Walk pages until the reported total is collected.
        var items = new List<T>();
        for (var pageNumber = 1; pageNumber <= MaximumPages; pageNumber++)
        {
            var page = await loadPage(pageNumber, cancellationToken).ConfigureAwait(false);
            items.AddRange(page.Items);
            if (page.Items.Count == 0 || items.Count >= page.TotalCount)
            {
                break;
            }
        }

        return items;
    }
}
