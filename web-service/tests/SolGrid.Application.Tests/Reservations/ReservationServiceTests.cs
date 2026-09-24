/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationServiceTests.cs
 * Description: Verifies energy reservation creation business rules.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Services;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Application.Tests.Reservations;

public sealed class ReservationServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("operator-1")]
    [InlineData("prosumer-1")]
    public async Task GridOperator_CannotMutateReservations_EvenWithOwnerSubject(string subject)
    {
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation), currentUserContext: new FakeCurrentUserContext(subject, UserRole.GridOperator));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(2))));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateReservationAsync(reservation.Id, CreateUpdateRequest(CurrentTime.AddHours(14))));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelReservationAsync(reservation.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ApproveReservationAsync(reservation.Id, new ApproveReservationRequest()));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.RejectReservationAsync(reservation.Id, new RejectReservationRequest { RejectionReason = "Maintenance" }));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.IssueReservationQrAsync(reservation.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.VerifyReservationQrAsync(new VerifyReservationQrRequest { ReservationId = reservation.Id, VerificationToken = "valid-token" }));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CompleteReservationAsync(reservation.Id, new CompleteReservationRequest { VerificationToken = "valid-token" }));

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(CurrentTime.AddHours(13), reservation.ScheduledAt);
        Assert.Null(reservation.QrVerificationTokenHash);
    }

    [Fact]
    public async Task GridOperator_CanReadReservationDetailsAndList()
    {
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var profile = new ProsumerResponse
        {
            Nic = reservation.ProsumerId, FirstName = "Nimal", LastName = "Perera",
            Email = "nimal@example.com", PhoneNumber = "0712345678",
            Status = ProsumerAccountStatus.Active, CreatedAt = CurrentTime, UpdatedAt = CurrentTime
        };
        var service = CreateService(new InMemoryReservationRepository(reservation),
            currentUserContext: new FakeCurrentUserContext("operator-1", UserRole.GridOperator),
            prosumerReadService: new FakeProsumerReadService(new ReservationProsumerSnapshot
            {
                Id = profile.Nic, Nic = profile.Nic, FullName = "Nimal Perera", IsActive = true, Details = profile
            }));

        var details = await service.GetReservationByIdAsync(reservation.Id);
        Assert.Equal(reservation.Id, details.Id);
        Assert.Equal(reservation.ProsumerId, details.ProsumerDetails!.Nic);
        Assert.Equal(profile.Email, details.ProsumerDetails.Email);
        Assert.Equal(profile.PhoneNumber, details.ProsumerDetails.PhoneNumber);
        var listed = Assert.Single((await service.GetReservationsAsync(new ReservationQuery())).Items);
        Assert.Equal(reservation.Id, listed.Id);
        Assert.Null(listed.ProsumerDetails);
    }

    [Fact]
    public async Task Backoffice_CanCreateEditAndCancelBookings()
    {
        var repository = new InMemoryReservationRepository();
        var service = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var created = await service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(13)));
        Assert.Equal("prosumer-1", created.ProsumerId);
        var updated = await service.UpdateReservationAsync(created.Id, CreateUpdateRequest(CurrentTime.AddHours(14)));
        Assert.Equal(CurrentTime.AddHours(14), updated.ScheduledAt);
        await service.CancelReservationAsync(created.Id);
        Assert.Equal(ReservationStatus.Cancelled, (await repository.GetByIdAsync(created.Id))!.Status);
    }

    [Fact]
    public async Task CreateReservationAsync_WithValidRequest_CreatesPendingReservation()
    {
        // Verify a valid request creates and persists a pending reservation.
        var repository = new InMemoryReservationRepository();
        var service = CreateService(repository);

        var response = await service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(2)));

        var persistedReservation = await repository.GetByIdAsync(response.Id);
        Assert.NotNull(persistedReservation);
        Assert.Equal(ReservationStatus.Pending, response.Status);
        Assert.Equal(CurrentTime.AddHours(2), response.ScheduledAt);
        Assert.True(response.CreatedAt.Offset == TimeSpan.Zero);
    }

    [Fact]
    public async Task CreateReservationAsync_WithPastSchedule_ThrowsValidation()
    {
        // Verify past reservation times are rejected.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddMinutes(-1))));
    }

    [Fact]
    public async Task CreateReservationAsync_ExactlyWithinSevenDayWindow_CreatesReservation()
    {
        // Verify the assignment seven-day boundary is inclusive.
        var service = CreateService();

        var response = await service.CreateReservationAsync(CreateRequest(CurrentTime.AddDays(7)));

        Assert.Equal(CurrentTime.AddDays(7), response.ScheduledAt);
    }

    [Fact]
    public async Task CreateReservationAsync_BeyondSevenDayWindow_ThrowsValidation()
    {
        // Verify reservations after the allowed assignment window are rejected.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddDays(7).AddTicks(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithMissingProsumer_ThrowsNotFound()
    {
        // Verify missing prosumer references are reported as not found.
        var prosumers = new FakeProsumerReadService();
        var service = CreateService(prosumerReadService: prosumers);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveProsumer_ThrowsConflict()
    {
        // Verify inactive prosumers cannot create reservations.
        var prosumers = new FakeProsumerReadService(new ReservationProsumerSnapshot
        {
            Id = "prosumer-1",
            IsActive = false
        });
        var service = CreateService(prosumerReadService: prosumers);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveStation_ThrowsConflict()
    {
        // Verify inactive stations cannot be reserved.
        var stations = new FakeStationReadService(new ReservationStationSnapshot
        {
            Id = "station-1",
            IsActive = false
        });
        var service = CreateService(stationReadService: stations);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInvalidSlot_ThrowsNotFound()
    {
        // Verify missing booking slots are reported as not found.
        var slots = new FakeBookingSlotReadService();
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WhenSlotBelongsToAnotherStation_ThrowsValidation()
    {
        // Verify booking slots must belong to the requested station.
        var slots = new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
        {
            Id = "slot-1",
            StationId = "station-2",
            IsActive = true,
            IsAvailable = true
        });
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithUnavailableSlot_ThrowsConflict()
    {
        // Verify unavailable booking slots cannot be reserved.
        var slots = new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
        {
            Id = "slot-1",
            StationId = "station-1",
            IsActive = true,
            IsAvailable = false
        });
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithDuplicateActiveReservation_ThrowsConflict()
    {
        // Verify one booking slot cannot have two active reservations.
        var existingReservation = EnergyReservation.Create(
            "reservation-existing",
            "other-prosumer",
            "station-1",
            "slot-1",
            CurrentTime.AddHours(1),
            CurrentTime);
        var repository = new InMemoryReservationRepository(existingReservation);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(2))));
    }

    [Fact]
    public async Task UpdateReservationAsync_WithMoreThanTwelveHoursRemaining_UpdatesReservation()
    {
        // Verify owned active reservations can be updated outside the notice window.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var repository = new InMemoryReservationRepository(reservation);
        var service = CreateService(
            repository,
            bookingSlotReadService: new FakeBookingSlotReadService(
                CreateSlot("slot-1", "station-1"),
                CreateSlot("slot-2", "station-1")));

        var response = await service.UpdateReservationAsync("reservation-1", new UpdateReservationRequest
        {
            StationId = "station-1",
            BookingSlotId = "slot-2",
            ScheduledAt = CurrentTime.AddHours(14)
        });

        Assert.Equal("slot-2", response.BookingSlotId);
        Assert.Equal(CurrentTime.AddHours(14), response.ScheduledAt);
    }

    [Fact]
    public async Task UpdateReservationAsync_InsideTwelveHours_ThrowsConflict()
    {
        // Verify updates inside the assignment notice period are rejected.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(11).AddMinutes(59));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateReservationAsync("reservation-1", CreateUpdateRequest(CurrentTime.AddHours(13))));
    }

    [Fact]
    public async Task UpdateReservationAsync_ExactlyAtTwelveHourBoundary_UpdatesReservation()
    {
        // Verify the twelve-hour notice boundary is inclusive.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(12));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        var response = await service.UpdateReservationAsync("reservation-1", CreateUpdateRequest(CurrentTime.AddHours(13)));

        Assert.Equal(CurrentTime.AddHours(13), response.ScheduledAt);
    }

    [Fact]
    public async Task CancelReservationAsync_WithMoreThanTwelveHoursRemaining_CancelsReservation()
    {
        // Verify owned active reservations can be cancelled outside the notice window.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await service.CancelReservationAsync("reservation-1");

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(CurrentTime, reservation.CancelledAt);
    }

    [Fact]
    public async Task CancelReservationAsync_InsideTwelveHours_ThrowsConflict()
    {
        // Verify cancellations inside the assignment notice period are rejected.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(11));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ConflictException>(() => service.CancelReservationAsync("reservation-1"));
    }

    [Fact]
    public async Task UpdateReservationAsync_ForAnotherProsumer_ThrowsForbidden()
    {
        // Verify a prosumer cannot modify another prosumer's reservation.
        var reservation = CreateReservation("reservation-1", "prosumer-2", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateReservationAsync("reservation-1", CreateUpdateRequest(CurrentTime.AddHours(14))));
    }

    [Fact]
    public async Task CancelReservationAsync_ForAnotherProsumer_ThrowsForbidden()
    {
        // Verify a prosumer cannot cancel another prosumer's reservation.
        var reservation = CreateReservation("reservation-1", "prosumer-2", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelReservationAsync("reservation-1"));
    }

    [Fact]
    public async Task UpdateReservationAsync_ForCompletedReservation_ThrowsConflict()
    {
        // Verify completed reservations cannot be modified.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        reservation.Approve("backoffice-1", CurrentTime);
        reservation.Complete("operator-1", CurrentTime.AddMinutes(10));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateReservationAsync("reservation-1", CreateUpdateRequest(CurrentTime.AddHours(14))));
    }

    [Fact]
    public async Task CancelReservationAsync_ForCompletedReservation_ThrowsConflict()
    {
        // Verify completed reservations cannot be cancelled.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        reservation.Approve("backoffice-1", CurrentTime);
        reservation.Complete("operator-1", CurrentTime.AddMinutes(10));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ConflictException>(() => service.CancelReservationAsync("reservation-1"));
    }

    [Fact]
    public async Task UpdateReservationAsync_ForRejectedOrCancelledReservation_ThrowsConflict()
    {
        // Verify rejected and cancelled reservations cannot be modified.
        var rejectedReservation = CreateReservation("reservation-rejected", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        rejectedReservation.Reject("backoffice-1", "Unavailable.", CurrentTime);
        var cancelledReservation = CreateReservation("reservation-cancelled", "prosumer-1", "station-1", "slot-2", CurrentTime.AddHours(13));
        cancelledReservation.Cancel(CurrentTime);
        var service = CreateService(new InMemoryReservationRepository(rejectedReservation, cancelledReservation));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateReservationAsync("reservation-rejected", CreateUpdateRequest(CurrentTime.AddHours(14))));
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateReservationAsync("reservation-cancelled", CreateUpdateRequest(CurrentTime.AddHours(14))));
    }

    [Fact]
    public async Task GetReservationsAsync_WithFiltersAndPaging_ReturnsMatchingPage()
    {
        // Verify operational users can query filtered and paged reservations.
        var first = CreateReservation("reservation-first", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var second = CreateReservation("reservation-second", "prosumer-2", "station-2", "slot-2", CurrentTime.AddHours(14));
        var service = CreateService(
            new InMemoryReservationRepository(first, second),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.GetReservationsAsync(new ReservationQuery
        {
            StationId = "station-1",
            SearchText = "first",
            PageNumber = 1,
            PageSize = 10
        });

        var reservation = Assert.Single(response.Items);
        Assert.Equal("reservation-first", reservation.Id);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task GetMyReservationsAsync_FiltersToCurrentUser()
    {
        // Verify owned reservation queries ignore client-supplied prosumer filters.
        var own = CreateReservation("reservation-own", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var other = CreateReservation("reservation-other", "prosumer-2", "station-1", "slot-2", CurrentTime.AddHours(14));
        var service = CreateService(new InMemoryReservationRepository(own, other));

        var response = await service.GetMyReservationsAsync(new ReservationQuery
        {
            ProsumerId = "prosumer-2",
            PageNumber = 1,
            PageSize = 10
        });

        var reservation = Assert.Single(response.Items);
        Assert.Equal("reservation-own", reservation.Id);
    }

    [Fact]
    public async Task GetDashboardReservationsAsync_CurrentView_ReturnsOnlyFutureApprovedReservations()
    {
        // Verify the approved dashboard view excludes pending, historical, and closed reservations.
        var approved = CreateReservation("reservation-approved", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        approved.Approve("backoffice-1", CurrentTime);
        var pending = CreateReservation("reservation-pending", "prosumer-2", "station-1", "slot-2", CurrentTime.AddHours(14));
        var historical = CreateReservation("reservation-history", "prosumer-3", "station-1", "slot-3", CurrentTime.AddHours(-1));
        historical.Approve("backoffice-1", CurrentTime.AddHours(-2));
        var cancelled = CreateReservation("reservation-cancelled", "prosumer-4", "station-1", "slot-4", CurrentTime.AddHours(13));
        cancelled.Cancel(CurrentTime);
        var service = CreateService(
            new InMemoryReservationRepository(approved, pending, historical, cancelled),
            currentUserContext: new FakeCurrentUserContext("operator-1", UserRole.GridOperator));

        var response = await service.GetDashboardReservationsAsync(
            ReservationDashboardView.Current,
            new ReservationQuery { PageNumber = 1, PageSize = 10 });

        var reservation = Assert.Single(response.Items);
        Assert.Equal("reservation-approved", reservation.Id);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ReturnsAuthoritativeCounts()
    {
        // Verify dashboard counts are computed from reservation state without returning all records.
        var pending = CreateReservation("reservation-pending", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var approved = CreateReservation("reservation-approved", "prosumer-2", "station-1", "slot-2", CurrentTime.AddHours(14));
        approved.Approve("backoffice-1", CurrentTime);
        var completed = CreateReservation("reservation-completed", "prosumer-3", "station-1", "slot-3", CurrentTime.AddHours(13));
        completed.Approve("backoffice-1", CurrentTime);
        completed.Complete("operator-1", CurrentTime);
        var service = CreateService(
            new InMemoryReservationRepository(pending, approved, completed),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.GetDashboardSummaryAsync();

        Assert.Equal(1, response.PendingReservationsCount);
        Assert.Equal(1, response.ApprovedFutureReservationsCount);
        Assert.Equal(2, response.CurrentReservationsCount);
        Assert.Equal(1, response.BookingHistoryCount);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WithoutOperationalRole_ThrowsForbidden()
    {
        // Verify dashboard counts are not exposed to non-operational callers.
        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetDashboardSummaryAsync());
    }

    [Fact]
    public async Task ApproveReservationAsync_WithPendingReservation_ApprovesReservation()
    {
        // Verify Backoffice users can approve pending reservations.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(
            new InMemoryReservationRepository(reservation),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.ApproveReservationAsync("reservation-1", new ApproveReservationRequest());

        Assert.Equal(ReservationStatus.Approved, response.Status);
        Assert.Equal("backoffice-1", response.ApprovedBy);
        Assert.Equal(CurrentTime, response.ApprovedAt);
    }

    [Fact]
    public async Task RejectReservationAsync_WithPendingReservation_RejectsReservation()
    {
        // Verify operational users can reject pending reservations with a reason.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(
            new InMemoryReservationRepository(reservation),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.RejectReservationAsync("reservation-1", new RejectReservationRequest
        {
            RejectionReason = "Station maintenance."
        });

        Assert.Equal(ReservationStatus.Rejected, response.Status);
        Assert.Equal("backoffice-1", response.RejectedBy);
        Assert.Equal("Station maintenance.", response.RejectionReason);
    }

    [Fact]
    public async Task ApproveReservationAsync_WithoutOperationalRole_ThrowsForbidden()
    {
        // Verify non-operational callers cannot approve reservations.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ApproveReservationAsync("reservation-1", new ApproveReservationRequest()));
    }

    [Fact]
    public async Task ApproveReservationAsync_ForCancelledReservation_ThrowsConflict()
    {
        // Verify cancelled reservations cannot be approved.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        reservation.Cancel(CurrentTime);
        var service = CreateService(
            new InMemoryReservationRepository(reservation),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ApproveReservationAsync("reservation-1", new ApproveReservationRequest()));
    }

    [Fact]
    public async Task IssueReservationQrAsync_ForPendingReservation_ThrowsConflict()
    {
        // Verify QR payloads are available only after approval.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        var service = CreateService(new InMemoryReservationRepository(reservation));

        await Assert.ThrowsAsync<ConflictException>(() => service.IssueReservationQrAsync("reservation-1"));
    }

    [Fact]
    public async Task VerifyReservationQrAsync_WithValidToken_ReturnsValid()
    {
        // Verify approved reservation QR tokens can be validated server-side.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var ownerService = CreateService(repository);
        var qr = await ownerService.IssueReservationQrAsync("reservation-1");
        var backofficeService = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await backofficeService.VerifyReservationQrAsync(new VerifyReservationQrRequest
        {
            ReservationId = "reservation-1",
            VerificationToken = qr.VerificationToken
        });

        Assert.True(response.IsValid);
        Assert.Equal(CurrentTime, reservation.QrVerifiedAt);
    }

    [Fact]
    public async Task VerifyReservationQrAsync_WithInvalidToken_ReturnsInvalid()
    {
        // Verify mismatched QR tokens do not validate.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        await CreateService(repository).IssueReservationQrAsync("reservation-1");
        var backofficeService = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await backofficeService.VerifyReservationQrAsync(new VerifyReservationQrRequest
        {
            ReservationId = "reservation-1",
            VerificationToken = "wrong-token"
        });

        Assert.False(response.IsValid);
    }

    [Fact]
    public async Task VerifyReservationQrAsync_ForPendingReservation_ReturnsInvalid()
    {
        // Verify pending reservations cannot pass QR verification.
        var reservation = CreateRestoredReservationWithQrToken(ReservationStatus.Pending);
        var service = CreateService(
            new InMemoryReservationRepository(reservation),
            currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.VerifyReservationQrAsync(new VerifyReservationQrRequest
        {
            ReservationId = "reservation-1",
            VerificationToken = "valid-token"
        });

        Assert.False(response.IsValid);
    }

    [Fact]
    public async Task VerifyReservationQrAsync_ForCancelledReservation_ReturnsInvalid()
    {
        // Verify cancelled reservations cannot pass QR verification.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var qr = await CreateService(repository).IssueReservationQrAsync("reservation-1");
        reservation.Cancel(CurrentTime);
        var service = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.VerifyReservationQrAsync(new VerifyReservationQrRequest
        {
            ReservationId = "reservation-1",
            VerificationToken = qr.VerificationToken
        });

        Assert.False(response.IsValid);
    }

    [Fact]
    public async Task VerifyReservationQrAsync_ForCompletedReservation_ReturnsInvalid()
    {
        // Verify completed reservations cannot pass QR verification.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var qr = await CreateService(repository).IssueReservationQrAsync("reservation-1");
        reservation.Complete("backoffice-1", CurrentTime);
        var service = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await service.VerifyReservationQrAsync(new VerifyReservationQrRequest
        {
            ReservationId = "reservation-1",
            VerificationToken = qr.VerificationToken
        });

        Assert.False(response.IsValid);
    }

    [Fact]
    public async Task CompleteReservationAsync_WithValidToken_CompletesReservation()
    {
        // Verify Backoffice users can complete approved reservations with a valid QR token.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var qr = await CreateService(repository).IssueReservationQrAsync("reservation-1");
        var backofficeService = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        var response = await backofficeService.CompleteReservationAsync("reservation-1", new CompleteReservationRequest
        {
            VerificationToken = qr.VerificationToken
        });

        Assert.Equal(ReservationStatus.Completed, response.Status);
        Assert.Equal("backoffice-1", response.CompletedBy);
        Assert.Equal(CurrentTime, response.CompletedAt);
    }

    [Fact]
    public async Task CompleteReservationAsync_WithoutOperationalRole_ThrowsForbidden()
    {
        // Verify non-operational callers cannot complete reservations.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var qr = await CreateService(repository).IssueReservationQrAsync("reservation-1");
        var prosumerService = CreateService(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            prosumerService.CompleteReservationAsync("reservation-1", new CompleteReservationRequest
            {
                VerificationToken = qr.VerificationToken
            }));
    }

    [Fact]
    public async Task CompleteReservationAsync_WhenAlreadyCompleted_ThrowsConflict()
    {
        // Verify a reservation cannot be completed twice.
        var reservation = CreateApprovedReservation();
        var repository = new InMemoryReservationRepository(reservation);
        var qr = await CreateService(repository).IssueReservationQrAsync("reservation-1");
        var backofficeService = CreateService(repository, currentUserContext: new FakeCurrentUserContext("backoffice-1", UserRole.Backoffice));

        await backofficeService.CompleteReservationAsync("reservation-1", new CompleteReservationRequest
        {
            VerificationToken = qr.VerificationToken
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            backofficeService.CompleteReservationAsync("reservation-1", new CompleteReservationRequest
            {
                VerificationToken = qr.VerificationToken
            }));
    }

    [Fact]
    public async Task GetMyDashboardSummary_CountsOnlyOwnerAndFutureApprovedReservations()
    {
        // Verify ownership and the exact now boundary in server-calculated mobile counts.
        var future = CreateReservation("future", "prosumer-1", "station-1", "slot-1", CurrentTime);
        future.Approve("operator", CurrentTime);
        var past = CreateReservation("past", "prosumer-1", "station-1", "slot-2", CurrentTime.AddHours(-1));
        past.Approve("operator", CurrentTime);
        var pending = CreateReservation("pending", "prosumer-1", "station-1", "slot-3", CurrentTime.AddHours(2));
        var other = CreateReservation("other", "prosumer-2", "station-1", "slot-4", CurrentTime.AddHours(2));
        other.Approve("operator", CurrentTime);
        var summary = await CreateService(new InMemoryReservationRepository(future, past, pending, other)).GetMyDashboardSummaryAsync();
        Assert.Equal(1, summary.PendingReservationsCount);
        Assert.Equal(1, summary.ApprovedFutureReservationsCount);
        Assert.Equal(2, summary.CurrentReservationsCount);
        Assert.Equal(1, summary.BookingHistoryCount);
    }

    [Fact]
    public async Task GetMyDashboardSummary_RejectsWebUserIdentity()
    {
        // Reject operational tokens at the owner-only dashboard endpoint.
        var service = CreateService(currentUserContext: new FakeCurrentUserContext("operator", UserRole.GridOperator));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetMyDashboardSummaryAsync());
    }

    private static ReservationService CreateService(
        InMemoryReservationRepository? repository = null,
        FakeProsumerReadService? prosumerReadService = null,
        FakeStationReadService? stationReadService = null,
        FakeBookingSlotReadService? bookingSlotReadService = null,
        FakeCurrentUserContext? currentUserContext = null)
    {
        // Create a reservation service with deterministic test dependencies.
        return new ReservationService(
            repository ?? new InMemoryReservationRepository(),
            prosumerReadService ?? new FakeProsumerReadService(new ReservationProsumerSnapshot
            {
                Id = "prosumer-1",
                IsActive = true
            }),
            stationReadService ?? new FakeStationReadService(new ReservationStationSnapshot
            {
                Id = "station-1",
                IsActive = true
            }),
            bookingSlotReadService ?? new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
            {
                Id = "slot-1",
                StationId = "station-1",
                IsActive = true,
                IsAvailable = true,
                // Include the fixed clock so completion tests can exercise token and state checks.
                StartTime = CurrentTime,
                EndTime = CurrentTime.AddDays(7)
            }),
            new FakeReservationQrTokenService(),
            currentUserContext ?? new FakeCurrentUserContext("prosumer-1"),
            new FixedTimeProvider(CurrentTime));
    }

    private static CreateReservationRequest CreateRequest(DateTimeOffset scheduledAt)
    {
        // Create a valid reservation request for service tests.
        return new CreateReservationRequest
        {
            ProsumerId = "prosumer-1",
            StationId = "station-1",
            BookingSlotId = "slot-1",
            ScheduledAt = scheduledAt
        };
    }

    private static UpdateReservationRequest CreateUpdateRequest(DateTimeOffset scheduledAt)
    {
        // Create a valid reservation update request for service tests.
        return new UpdateReservationRequest
        {
            StationId = "station-1",
            BookingSlotId = "slot-1",
            ScheduledAt = scheduledAt
        };
    }

    private static EnergyReservation CreateReservation(
        string id,
        string prosumerId,
        string stationId,
        string bookingSlotId,
        DateTimeOffset scheduledAt)
    {
        // Create a valid domain reservation for service tests.
        return EnergyReservation.Create(
            id,
            prosumerId,
            stationId,
            bookingSlotId,
            scheduledAt,
            CurrentTime);
    }

    private static ReservationBookingSlotSnapshot CreateSlot(string id, string stationId)
    {
        // Create an available booking slot snapshot for service tests.
        return new ReservationBookingSlotSnapshot
        {
            Id = id,
            StationId = stationId,
            IsActive = true,
            IsAvailable = true
        };
    }

    private static EnergyReservation CreateApprovedReservation()
    {
        // Create an approved reservation for QR and completion tests.
        var reservation = CreateReservation("reservation-1", "prosumer-1", "station-1", "slot-1", CurrentTime.AddHours(13));
        reservation.Approve("backoffice-1", CurrentTime);
        return reservation;
    }

    private static EnergyReservation CreateRestoredReservationWithQrToken(ReservationStatus status)
    {
        // Restore a reservation with persisted QR token metadata for verification edge cases.
        return EnergyReservation.Restore(
            "reservation-1",
            "prosumer-1",
            "station-1",
            "slot-1",
            CurrentTime.AddHours(13),
            status,
            CurrentTime,
            CurrentTime,
            approvedAt: null,
            approvedBy: null,
            rejectedAt: null,
            rejectedBy: null,
            rejectionReason: null,
            cancelledAt: null,
            completedAt: null,
            completedBy: null,
            qrVerificationTokenHash: "hash:valid-token",
            qrVerificationTokenIssuedAt: CurrentTime,
            qrVerificationTokenExpiresAt: CurrentTime.AddMinutes(30),
            qrVerifiedAt: null);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset currentTime;

        public FixedTimeProvider(DateTimeOffset currentTime)
        {
            // Store the deterministic test clock value.
            this.currentTime = currentTime;
        }

        public override DateTimeOffset GetUtcNow()
        {
            // Return the deterministic UTC time for reservation rule tests.
            return currentTime;
        }
    }

    private sealed class FakeProsumerReadService : IReservationProsumerReadService
    {
        private readonly ReservationProsumerSnapshot? prosumer;

        public FakeProsumerReadService(ReservationProsumerSnapshot? prosumer = null)
        {
            // Store the optional prosumer snapshot for tests.
            this.prosumer = prosumer;
        }

        public Task<ReservationProsumerSnapshot?> GetByIdAsync(
            string prosumerId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured prosumer when ids match.
            return Task.FromResult(prosumer?.Id == prosumerId ? prosumer : null);
        }
    }

    private sealed class FakeStationReadService : IReservationStationReadService
    {
        private readonly ReservationStationSnapshot? station;

        public FakeStationReadService(ReservationStationSnapshot? station = null)
        {
            // Store the optional station snapshot for tests.
            this.station = station;
        }

        public Task<ReservationStationSnapshot?> GetByIdAsync(
            string stationId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured station when ids match.
            return Task.FromResult(station?.Id == stationId ? station : null);
        }
    }

    private sealed class FakeBookingSlotReadService : IReservationBookingSlotReadService
    {
        private readonly IReadOnlyList<ReservationBookingSlotSnapshot> bookingSlots;

        public FakeBookingSlotReadService(params ReservationBookingSlotSnapshot[] bookingSlots)
        {
            // Store the optional booking slot snapshots for tests.
            this.bookingSlots = bookingSlots;
        }

        public Task<ReservationBookingSlotSnapshot?> GetByIdAsync(
            string bookingSlotId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured booking slot when ids match.
            return Task.FromResult(bookingSlots.FirstOrDefault(bookingSlot => bookingSlot.Id == bookingSlotId));
        }

        public Task ReserveAsync(string bookingSlotId, CancellationToken cancellationToken = default)
        {
            // No-op: slot status transitions are covered by SolarStations component tests.
            return Task.CompletedTask;
        }

        public Task OccupyAsync(string bookingSlotId, CancellationToken cancellationToken = default)
        {
            // No-op: slot status transitions are covered by SolarStations component tests.
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(string bookingSlotId, CancellationToken cancellationToken = default)
        {
            // No-op: slot status transitions are covered by SolarStations component tests.
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(string? userId, UserRole? role = null)
        {
            // Store the current user identity for ownership tests.
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);

        public string? UserId { get; }

        public UserRole? Role { get; }
    }

    private sealed class FakeReservationQrTokenService : IReservationQrTokenService
    {
        public string GenerateToken()
        {
            // Return a deterministic raw token for service tests.
            return "valid-token";
        }

        public string HashToken(string token)
        {
            // Return a deterministic token hash for service tests.
            return $"hash:{token}";
        }

        public bool VerifyToken(string token, string tokenHash)
        {
            // Verify the deterministic token hash for service tests.
            return tokenHash == HashToken(token);
        }
    }

    private sealed class InMemoryReservationRepository : IReservationRepository
    {
        private readonly List<EnergyReservation> reservations;

        public InMemoryReservationRepository(params EnergyReservation[] reservations)
        {
            // Store test reservations in memory.
            this.reservations = reservations.ToList();
        }

        public Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a reservation by id in memory.
            return Task.FromResult(reservations.FirstOrDefault(reservation => reservation.Id == id));
        }

        public Task<IReadOnlyList<EnergyReservation>> GetByProsumerIdAsync(
            string prosumerId,
            CancellationToken cancellationToken = default)
        {
            // Return reservations for one prosumer in memory.
            return Task.FromResult<IReadOnlyList<EnergyReservation>>(
                reservations.Where(reservation => reservation.ProsumerId == prosumerId).ToArray());
        }

        public Task<PagedResult<EnergyReservation>> GetPagedAsync(
            ReservationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Return filtered in-memory reservations in a page.
            return Task.FromResult(CreatePage(reservations, query));
        }

        public Task<PagedResult<EnergyReservation>> GetDashboardReservationsAsync(
            ReservationDashboardView view,
            ReservationQuery query,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            // Return an in-memory approximation of the requested server dashboard view.
            var dashboardReservations = view switch
            {
                ReservationDashboardView.Current => reservations.Where(reservation =>
                    reservation.Status == ReservationStatus.Approved && reservation.ScheduledAt >= nowUtc),
                ReservationDashboardView.Pending => reservations.Where(reservation =>
                    reservation.Status == ReservationStatus.Pending),
                ReservationDashboardView.History => reservations.Where(reservation =>
                    !reservation.IsActive || reservation.ScheduledAt < nowUtc),
                _ => throw new ArgumentOutOfRangeException(nameof(view))
            };

            return Task.FromResult(CreatePage(dashboardReservations, query));
        }

        public Task<ReservationDashboardCounts> GetDashboardCountsAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            // Calculate dashboard counts in memory for application service tests.
            return Task.FromResult(new ReservationDashboardCounts
            {
                PendingReservationsCount = reservations.LongCount(reservation => reservation.Status == ReservationStatus.Pending),
                ApprovedFutureReservationsCount = reservations.LongCount(reservation =>
                    reservation.Status == ReservationStatus.Approved && reservation.ScheduledAt >= nowUtc),
                CurrentReservationsCount = reservations.LongCount(reservation =>
                    reservation.IsActive && reservation.ScheduledAt >= nowUtc),
                BookingHistoryCount = reservations.LongCount(reservation =>
                    !reservation.IsActive || reservation.ScheduledAt < nowUtc)
            });
        }

        private static PagedResult<EnergyReservation> CreatePage(
            IEnumerable<EnergyReservation> source,
            ReservationQuery query)
        {
            // Apply common in-memory filters and paging for repository test doubles.
            IEnumerable<EnergyReservation> queryableReservations = source;

            if (!string.IsNullOrWhiteSpace(query.ProsumerId))
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.ProsumerId == query.ProsumerId);
            }

            if (!string.IsNullOrWhiteSpace(query.StationId))
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.StationId == query.StationId);
            }

            if (!string.IsNullOrWhiteSpace(query.BookingSlotId))
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.BookingSlotId == query.BookingSlotId);
            }

            if (query.Status.HasValue)
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                queryableReservations = queryableReservations.Where(reservation =>
                    reservation.Id.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)
                    || reservation.ProsumerId.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)
                    || reservation.StationId.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)
                    || reservation.BookingSlotId.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (query.ScheduledFrom.HasValue)
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.ScheduledAt >= query.ScheduledFrom);
            }

            if (query.ScheduledTo.HasValue)
            {
                queryableReservations = queryableReservations.Where(reservation => reservation.ScheduledAt <= query.ScheduledTo);
            }

            var pageNumber = Math.Max(query.PageNumber, 1);
            var pageSize = Math.Max(query.PageSize, 1);
            var filteredReservations = queryableReservations.ToArray();
            return new PagedResult<EnergyReservation>
            {
                Items = filteredReservations.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray(),
                TotalCount = filteredReservations.Length,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public Task<bool> HasActiveReservationForBookingSlotAsync(
            string bookingSlotId,
            string? excludingReservationId = null,
            CancellationToken cancellationToken = default)
        {
            // Check for active reservations assigned to the same booking slot.
            return Task.FromResult(reservations.Any(reservation =>
                reservation.BookingSlotId == bookingSlotId
                && reservation.IsActive
                && reservation.Id != excludingReservationId));
        }

        public Task<bool> HasActiveReservationsForStationAsync(
            string stationId,
            CancellationToken cancellationToken = default)
        {
            // Check for active reservations assigned to the same station.
            return Task.FromResult(reservations.Any(reservation =>
                reservation.StationId == stationId && reservation.IsActive));
        }

        public Task AddAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
        {
            // Add a reservation to the in-memory store.
            reservations.Add(reservation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
        {
            // Updates are not required for create reservation tests.
            return Task.CompletedTask;
        }
    }
}
