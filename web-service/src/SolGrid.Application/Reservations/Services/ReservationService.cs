/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationService.cs
 * Description: Handles energy reservation business use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Reservations.Services;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository reservationRepository;
    private readonly IReservationProsumerReadService prosumerReadService;
    private readonly IReservationStationReadService stationReadService;
    private readonly IReservationBookingSlotReadService bookingSlotReadService;
    private readonly IReservationQrTokenService qrTokenService;
    private readonly ICurrentUserContext currentUserContext;
    private readonly TimeProvider timeProvider;

    public ReservationService(
        IReservationRepository reservationRepository,
        IReservationProsumerReadService prosumerReadService,
        IReservationStationReadService stationReadService,
        IReservationBookingSlotReadService bookingSlotReadService,
        IReservationQrTokenService qrTokenService,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        // Capture reservation workflow dependencies.
        this.reservationRepository = reservationRepository;
        this.prosumerReadService = prosumerReadService;
        this.stationReadService = stationReadService;
        this.bookingSlotReadService = bookingSlotReadService;
        this.qrTokenService = qrTokenService;
        this.currentUserContext = currentUserContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ReservationResponse> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate references, enforce slot availability rules, and persist a pending reservation.
        ValidateCreateRequest(request);

        var prosumerId = ResolveProsumerIdForCreate(request.ProsumerId);
        var stationId = NormalizeIdentifier(request.StationId);
        var bookingSlotId = NormalizeIdentifier(request.BookingSlotId);
        var scheduledAtUtc = ReservationTimeRules.NormalizeToUtc(request.ScheduledAt);
        var nowUtc = timeProvider.GetUtcNow();

        ValidateSchedule(scheduledAtUtc, nowUtc);

        await EnsureProsumerCanReserveAsync(prosumerId, cancellationToken).ConfigureAwait(false);
        await EnsureStationCanBeReservedAsync(stationId, cancellationToken).ConfigureAwait(false);
        await EnsureBookingSlotCanBeReservedAsync(bookingSlotId, stationId, cancellationToken).ConfigureAwait(false);

        if (await reservationRepository
                .HasActiveReservationForBookingSlotAsync(bookingSlotId, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException("Booking slot already has an active reservation.");
        }

        var reservation = EnergyReservation.Create(
            Guid.NewGuid().ToString("N"),
            prosumerId,
            stationId,
            bookingSlotId,
            scheduledAtUtc,
            nowUtc);

        await reservationRepository.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    public async Task<ReservationResponse> UpdateReservationAsync(
        string id,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate ownership, notice period, slot availability, and reschedule an active reservation.
        ValidateId(id, "Reservation id is required.");
        ValidateUpdateRequest(request);

        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsureCanAccessReservation(reservation);
        EnsureReservationCanChange(reservation, "Reservation status does not permit modification.");

        var nowUtc = timeProvider.GetUtcNow();
        EnsureChangeNotice(reservation, nowUtc, "Reservation updates require at least 12 hours notice.");

        var stationId = NormalizeIdentifier(request.StationId);
        var bookingSlotId = NormalizeIdentifier(request.BookingSlotId);
        var scheduledAtUtc = ReservationTimeRules.NormalizeToUtc(request.ScheduledAt);

        ValidateSchedule(scheduledAtUtc, nowUtc);
        await EnsureStationCanBeReservedAsync(stationId, cancellationToken).ConfigureAwait(false);
        await EnsureBookingSlotCanBeReservedAsync(bookingSlotId, stationId, cancellationToken).ConfigureAwait(false);

        if (await reservationRepository
                .HasActiveReservationForBookingSlotAsync(bookingSlotId, reservation.Id, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new ConflictException("Booking slot already has an active reservation.");
        }

        reservation.Reschedule(stationId, bookingSlotId, scheduledAtUtc, nowUtc);
        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    public async Task CancelReservationAsync(string id, CancellationToken cancellationToken = default)
    {
        // Validate ownership, notice period, and cancel an active reservation without deleting it.
        ValidateId(id, "Reservation id is required.");

        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsureCanAccessReservation(reservation);
        EnsureReservationCanChange(reservation, "Reservation status does not permit cancellation.");

        var nowUtc = timeProvider.GetUtcNow();
        EnsureChangeNotice(reservation, nowUtc, "Reservation cancellations require at least 12 hours notice.");

        reservation.Cancel(nowUtc);
        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReservationResponse> GetReservationByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Return a reservation when the authenticated caller can access it.
        ValidateId(id, "Reservation id is required.");
        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsureCanAccessReservation(reservation);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    public async Task<PagedResult<ReservationResponse>> GetReservationsAsync(
        ReservationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return filtered reservations for operational roles only.
        EnsureCanListReservations();
        ValidateQuery(query);
        var reservations = await reservationRepository.GetPagedAsync(query, cancellationToken).ConfigureAwait(false);
        return MapPage(reservations);
    }

    public async Task<PagedResult<ReservationResponse>> GetMyReservationsAsync(
        ReservationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return only reservations owned by the current authenticated user.
        var currentUserId = GetRequiredCurrentUserId();
        ValidateQuery(query);

        var ownerQuery = new ReservationQuery
        {
            ProsumerId = currentUserId,
            StationId = query.StationId,
            BookingSlotId = query.BookingSlotId,
            Status = query.Status,
            SearchText = query.SearchText,
            ScheduledFrom = query.ScheduledFrom,
            ScheduledTo = query.ScheduledTo,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        var reservations = await reservationRepository.GetPagedAsync(ownerQuery, cancellationToken).ConfigureAwait(false);
        return MapPage(reservations);
    }

    public async Task<PagedResult<ReservationResponse>> GetDashboardReservationsAsync(
        ReservationDashboardView view,
        ReservationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return one server-filtered and paged operational dashboard reservation view.
        EnsureCanListReservations();
        ValidateDashboardView(view);
        ValidateQuery(query);

        var reservations = await reservationRepository
            .GetDashboardReservationsAsync(view, query, timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        return MapPage(reservations);
    }

    public async Task<ReservationDashboardSummaryResponse> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        // Return authoritative operational dashboard counts without loading reservation documents.
        EnsureCanListReservations();
        var counts = await reservationRepository
            .GetDashboardCountsAsync(timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);

        return new ReservationDashboardSummaryResponse
        {
            PendingReservationsCount = counts.PendingReservationsCount,
            ApprovedFutureReservationsCount = counts.ApprovedFutureReservationsCount,
            CurrentReservationsCount = counts.CurrentReservationsCount,
            BookingHistoryCount = counts.BookingHistoryCount
        };
    }

    public async Task<ReservationResponse> ApproveReservationAsync(
        string id,
        ApproveReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Approve a pending reservation with authenticated reviewer metadata.
        ValidateId(id, "Reservation id is required.");
        EnsureCanReviewReservations();

        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsurePendingForReview(reservation, "Only pending reservations can be approved.");

        try
        {
            reservation.Approve(GetRequiredCurrentUserId(), timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    public async Task<ReservationResponse> RejectReservationAsync(
        string id,
        RejectReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Reject a pending reservation with authenticated reviewer metadata and a validated reason.
        ValidateId(id, "Reservation id is required.");
        ValidateRejectRequest(request);
        EnsureCanReviewReservations();

        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsurePendingForReview(reservation, "Only pending reservations can be rejected.");

        try
        {
            reservation.Reject(GetRequiredCurrentUserId(), request.RejectionReason, timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    public async Task<ReservationQrResponse> IssueReservationQrAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Issue a new short-lived QR token for an approved reservation.
        ValidateId(id, "Reservation id is required.");
        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        EnsureCanIssueQr(reservation);
        EnsureApprovedForQr(reservation, "QR payload is available only for approved reservations.");

        var issuedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = ReservationTimeRules.GetQrVerificationTokenExpiry(issuedAtUtc);
        var token = qrTokenService.GenerateToken();
        reservation.RegisterQrVerificationToken(qrTokenService.HashToken(token), issuedAtUtc, expiresAtUtc);

        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        return new ReservationQrResponse
        {
            ReservationId = reservation.Id,
            VerificationToken = token,
            ExpiresAt = expiresAtUtc
        };
    }

    public async Task<VerifyReservationQrResponse> VerifyReservationQrAsync(
        VerifyReservationQrRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify a QR token against the server-side reservation token hash.
        ValidateVerifyQrRequest(request);
        EnsureCanVerifyQr();

        var reservationId = NormalizeIdentifier(request.ReservationId);
        var reservation = await GetRequiredReservationAsync(reservationId, cancellationToken).ConfigureAwait(false);

        if (!IsQrUsable(reservation, request.VerificationToken, timeProvider.GetUtcNow()))
        {
            return new VerifyReservationQrResponse
            {
                IsValid = false,
                ReservationId = reservation.Id,
                Message = "QR verification failed."
            };
        }

        reservation.MarkQrVerified(timeProvider.GetUtcNow());
        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);

        return new VerifyReservationQrResponse
        {
            IsValid = true,
            ReservationId = reservation.Id,
            Message = "QR verification succeeded."
        };
    }

    public async Task<ReservationResponse> CompleteReservationAsync(
        string id,
        CompleteReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Complete an approved reservation after revalidating the QR token server-side.
        ValidateId(id, "Reservation id is required.");
        ValidateCompleteRequest(request);
        EnsureCanCompleteReservation();

        var reservation = await GetRequiredReservationAsync(id, cancellationToken).ConfigureAwait(false);
        var nowUtc = timeProvider.GetUtcNow();

        if (!IsQrUsable(reservation, request.VerificationToken, nowUtc))
        {
            throw new ConflictException("Reservation QR token is not valid for completion.");
        }

        try
        {
            reservation.Complete(GetRequiredCurrentUserId(), nowUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await reservationRepository.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        return ReservationResponseMapper.ToResponse(reservation);
    }

    private static void ValidateCreateRequest(CreateReservationRequest request)
    {
        // Validate required reservation create request fields.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.StationId))
        {
            errors.Add("Station id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.BookingSlotId))
        {
            errors.Add("Booking slot id is required.");
        }

        if (request.ScheduledAt == default)
        {
            errors.Add("Scheduled date and time is required.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateUpdateRequest(UpdateReservationRequest request)
    {
        // Validate editable reservation update request fields.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.StationId))
        {
            errors.Add("Station id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.BookingSlotId))
        {
            errors.Add("Booking slot id is required.");
        }

        if (request.ScheduledAt == default)
        {
            errors.Add("Scheduled date and time is required.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateRejectRequest(RejectReservationRequest request)
    {
        // Validate rejection details before changing reservation state.
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            throw new ValidationException(["Rejection reason is required."]);
        }
    }

    private static void ValidateVerifyQrRequest(VerifyReservationQrRequest request)
    {
        // Validate QR verification request fields.
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ReservationId))
        {
            errors.Add("Reservation id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VerificationToken))
        {
            errors.Add("Verification token is required.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateCompleteRequest(CompleteReservationRequest request)
    {
        // Validate completion token before server-side transaction completion.
        if (string.IsNullOrWhiteSpace(request.VerificationToken))
        {
            throw new ValidationException(["Verification token is required."]);
        }
    }

    private static void ValidateSchedule(DateTimeOffset scheduledAtUtc, DateTimeOffset nowUtc)
    {
        // Validate assignment scheduling rules using centralized time-window helpers.
        var errors = new List<string>();

        if (ReservationTimeRules.IsScheduledInPast(scheduledAtUtc, nowUtc))
        {
            errors.Add("Reservation cannot be scheduled in the past.");
        }

        if (ReservationTimeRules.IsBeyondMaximumReservationWindow(scheduledAtUtc, nowUtc))
        {
            errors.Add($"Reservation must be scheduled within {ReservationTimeRules.MaximumAdvanceReservationDays} days.");
        }

        ThrowIfInvalid(errors);
    }

    private async Task EnsureProsumerCanReserveAsync(string prosumerId, CancellationToken cancellationToken)
    {
        // Verify the referenced prosumer exists and can make reservations.
        var prosumer = await prosumerReadService.GetByIdAsync(prosumerId, cancellationToken).ConfigureAwait(false);

        if (prosumer is null)
        {
            throw new NotFoundException("Prosumer", prosumerId);
        }

        if (!prosumer.IsActive)
        {
            throw new ConflictException("Prosumer account is not active for reservations.");
        }
    }

    private async Task EnsureStationCanBeReservedAsync(string stationId, CancellationToken cancellationToken)
    {
        // Verify the referenced station exists and is active.
        var station = await stationReadService.GetByIdAsync(stationId, cancellationToken).ConfigureAwait(false);

        if (station is null)
        {
            throw new NotFoundException("Station", stationId);
        }

        if (!station.IsActive)
        {
            throw new ConflictException("Station is not active for reservations.");
        }
    }

    private async Task EnsureBookingSlotCanBeReservedAsync(
        string bookingSlotId,
        string stationId,
        CancellationToken cancellationToken)
    {
        // Verify the booking slot exists, belongs to the station, and is available.
        var bookingSlot = await bookingSlotReadService.GetByIdAsync(bookingSlotId, cancellationToken).ConfigureAwait(false);

        if (bookingSlot is null)
        {
            throw new NotFoundException("BookingSlot", bookingSlotId);
        }

        if (!string.Equals(bookingSlot.StationId, stationId, StringComparison.Ordinal))
        {
            throw new ValidationException(["Booking slot does not belong to the requested station."]);
        }

        if (!bookingSlot.IsActive || !bookingSlot.IsAvailable)
        {
            throw new ConflictException("Booking slot is not available for reservations.");
        }
    }

    private async Task<EnergyReservation> GetRequiredReservationAsync(
        string id,
        CancellationToken cancellationToken)
    {
        // Load a reservation or report a client-safe not-found error.
        var reservation = await reservationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return reservation ?? throw new NotFoundException("Reservation", id);
    }

    private void EnsureCanAccessReservation(EnergyReservation reservation)
    {
        // Ensure callers can only access owned reservations unless their role permits administration.
        if (IsOperationalRole() || IsCurrentUserOwner(reservation))
        {
            return;
        }

        throw new ForbiddenException("Current user is not allowed to access this reservation.");
    }

    private void EnsureCanListReservations()
    {
        // Limit general reservation listing to operational web roles.
        if (!IsOperationalRole())
        {
            throw new ForbiddenException("Current user is not allowed to list reservations.");
        }
    }

    private void EnsureCanReviewReservations()
    {
        // Limit reservation approval and rejection to operational web roles.
        if (!IsOperationalRole())
        {
            throw new ForbiddenException("Current user is not allowed to review reservations.");
        }
    }

    private void EnsureCanVerifyQr()
    {
        // Limit QR verification to operational web roles.
        if (!IsOperationalRole())
        {
            throw new ForbiddenException("Current user is not allowed to verify reservation QR tokens.");
        }
    }

    private void EnsureCanIssueQr(EnergyReservation reservation)
    {
        // Allow QR token issuance only to the owner or Backoffice administrators.
        if (currentUserContext.Role == UserRole.Backoffice || IsCurrentUserOwner(reservation))
        {
            return;
        }

        throw new ForbiddenException("Current user is not allowed to issue reservation QR tokens.");
    }

    private void EnsureCanCompleteReservation()
    {
        // Limit completion to authenticated operational users.
        if (!IsOperationalRole())
        {
            throw new ForbiddenException("Current user is not allowed to complete reservations.");
        }
    }

    private void EnsureReservationCanChange(EnergyReservation reservation, string message)
    {
        // Reject modification workflows for completed, rejected, or cancelled reservations.
        if (!reservation.IsActive)
        {
            throw new ConflictException(message);
        }
    }

    private static void EnsurePendingForReview(EnergyReservation reservation, string message)
    {
        // Reject review workflows unless the reservation is still pending.
        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new ConflictException(message);
        }
    }

    private static void EnsureApprovedForQr(EnergyReservation reservation, string message)
    {
        // Reject QR operations unless the reservation is currently approved.
        if (reservation.Status != ReservationStatus.Approved)
        {
            throw new ConflictException(message);
        }
    }

    private static void EnsureChangeNotice(EnergyReservation reservation, DateTimeOffset nowUtc, string message)
    {
        // Enforce the assignment twelve-hour update and cancellation notice rule.
        if (!ReservationTimeRules.HasRequiredChangeNotice(reservation.ScheduledAt, nowUtc))
        {
            throw new ConflictException(message);
        }
    }

    private static void ValidateQuery(ReservationQuery query)
    {
        // Validate query filters before passing them to persistence.
        var errors = new List<string>();

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            errors.Add("Reservation status filter is not supported.");
        }

        if (query.ScheduledFrom.HasValue && query.ScheduledTo.HasValue && query.ScheduledFrom > query.ScheduledTo)
        {
            errors.Add("Scheduled from must be before scheduled to.");
        }

        if (query.PageNumber < 1)
        {
            errors.Add("Page number must be greater than zero.");
        }

        if (query.PageSize < 1)
        {
            errors.Add("Page size must be greater than zero.");
        }

        ThrowIfInvalid(errors);
    }

    private static void ValidateDashboardView(ReservationDashboardView view)
    {
        // Reject dashboard view values that have no server-side query definition.
        if (!Enum.IsDefined(view))
        {
            throw new ValidationException(["Reservation dashboard view is not supported."]);
        }
    }

    private static PagedResult<ReservationResponse> MapPage(PagedResult<EnergyReservation> reservations)
    {
        // Map a reservation page without exposing persistence or QR token hash details.
        return new PagedResult<ReservationResponse>
        {
            Items = reservations.Items.Select(ReservationResponseMapper.ToResponse).ToArray(),
            TotalCount = reservations.TotalCount,
            PageNumber = reservations.PageNumber,
            PageSize = reservations.PageSize
        };
    }

    private string ResolveProsumerIdForCreate(string requestedProsumerId)
    {
        // Resolve reservation ownership from claims unless a Backoffice caller is creating on behalf of a prosumer.
        if (currentUserContext.Role == UserRole.Backoffice)
        {
            if (string.IsNullOrWhiteSpace(requestedProsumerId))
            {
                throw new ValidationException(["Prosumer id is required."]);
            }

            return NormalizeIdentifier(requestedProsumerId);
        }

        return GetRequiredCurrentUserId();
    }

    private bool IsQrUsable(EnergyReservation reservation, string verificationToken, DateTimeOffset nowUtc)
    {
        // Check approved state, token presence, token expiry, and token hash match.
        if (reservation.Status != ReservationStatus.Approved
            || string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash)
            || !reservation.QrVerificationTokenExpiresAt.HasValue
            || reservation.QrVerificationTokenExpiresAt.Value <= nowUtc)
        {
            return false;
        }

        return qrTokenService.VerifyToken(verificationToken, reservation.QrVerificationTokenHash);
    }

    private bool IsOperationalRole()
    {
        // Check whether the current role can administer or view operational reservation records.
        return currentUserContext.Role is UserRole.Backoffice or UserRole.GridOperator;
    }

    private bool IsCurrentUserOwner(EnergyReservation reservation)
    {
        // Compare the reservation owner with the authenticated user id claim.
        return string.Equals(currentUserContext.UserId, reservation.ProsumerId, StringComparison.Ordinal);
    }

    private string GetRequiredCurrentUserId()
    {
        // Require an authenticated user id claim before ownership-based reservation actions.
        if (!currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(currentUserContext.UserId))
        {
            throw new ForbiddenException("Current user identity is required.");
        }

        return NormalizeIdentifier(currentUserContext.UserId);
    }

    private static void ValidateId(string id, string message)
    {
        // Validate that route ids contain meaningful text.
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException([message]);
        }
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

    private static string NormalizeIdentifier(string identifier)
    {
        // Trim identifier values before cross-component lookups and persistence.
        return identifier.Trim();
    }
}
