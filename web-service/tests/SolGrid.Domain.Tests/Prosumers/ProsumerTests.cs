/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerTests.cs
 * Description: Verifies prosumer account lifecycle invariants and transitions.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Domain.Tests.Prosumers;

public sealed class ProsumerTests
{
    [Fact]
    public void Create_WithRequiredProfile_CreatesPendingProsumerWithNormalizedNic()
    {
        // Verify registration starts pending and protects the normalized NIC business identifier.
        var createdAt = DateTimeOffset.UtcNow;

        var prosumer = Prosumer.Create(
            " 199012345678 ",
            "Nimal",
            "Perera",
            "Nimal@example.com",
            "071 234 5678",
            "hashed-password",
            createdAt);

        Assert.Equal("199012345678", prosumer.Nic);
        Assert.Equal("nimal@example.com", prosumer.Email);
        Assert.Equal(ProsumerAccountStatus.Pending, prosumer.Status);
        Assert.False(prosumer.IsActive);
    }

    [Fact]
    public void Activate_FromPending_MarksProsumerActive()
    {
        // Verify Backoffice activation can move a pending profile to active.
        var createdAt = DateTimeOffset.UtcNow;
        var prosumer = CreateProsumer(createdAt);

        prosumer.Activate(createdAt.AddMinutes(1));

        Assert.Equal(ProsumerAccountStatus.Active, prosumer.Status);
        Assert.True(prosumer.IsActive);
    }

    [Fact]
    public void RequestDeactivation_FromActive_MarksRequest()
    {
        // Verify only active profiles can enter the deactivation-request state.
        var createdAt = DateTimeOffset.UtcNow;
        var prosumer = CreateActiveProsumer(createdAt);

        prosumer.RequestDeactivation(createdAt.AddMinutes(2));

        Assert.Equal(ProsumerAccountStatus.DeactivationRequested, prosumer.Status);
        Assert.False(prosumer.IsActive);
    }

    [Fact]
    public void RequestDeactivation_FromPending_ThrowsInvalidOperationException()
    {
        // Verify a profile cannot request deactivation before it has become active.
        var prosumer = CreateProsumer(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => prosumer.RequestDeactivation(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Deactivate_FromDeactivationRequested_MarksProsumerDeactivated()
    {
        // Verify a requested account deactivation can be finalized without deleting history.
        var createdAt = DateTimeOffset.UtcNow;
        var prosumer = CreateActiveProsumer(createdAt);
        prosumer.RequestDeactivation(createdAt.AddMinutes(1));

        prosumer.Deactivate(createdAt.AddMinutes(2));

        Assert.Equal(ProsumerAccountStatus.Deactivated, prosumer.Status);
    }

    [Fact]
    public void Reactivate_FromDeactivated_MarksProsumerActive()
    {
        // Verify a deactivated profile can return to active only through the explicit transition.
        var createdAt = DateTimeOffset.UtcNow;
        var prosumer = CreateActiveProsumer(createdAt);
        prosumer.Deactivate(createdAt.AddMinutes(1));

        prosumer.Reactivate(createdAt.AddMinutes(2));

        Assert.Equal(ProsumerAccountStatus.Active, prosumer.Status);
    }

    [Fact]
    public void Reactivate_WhenNotDeactivated_ThrowsInvalidOperationException()
    {
        // Verify activation cannot bypass the required deactivated lifecycle state.
        var prosumer = CreateActiveProsumer(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => prosumer.Reactivate(DateTimeOffset.UtcNow));
    }

    private static Prosumer CreateProsumer(DateTimeOffset createdAt)
    {
        // Create a pending prosumer profile for lifecycle tests.
        return Prosumer.Create(
            "199012345678",
            "Nimal",
            "Perera",
            "nimal@example.com",
            "0712345678",
            "hashed-password",
            createdAt);
    }

    private static Prosumer CreateActiveProsumer(DateTimeOffset createdAt)
    {
        // Create an active prosumer profile for lifecycle tests.
        var prosumer = CreateProsumer(createdAt);
        prosumer.Activate(createdAt.AddMinutes(1));
        return prosumer;
    }
}
