using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeSlaMonitoringService : ISlaMonitoringService
{
    public IReadOnlyList<SlaExpiringNotification> ExpiringNotifications { get; set; } = [];

    public IReadOnlyList<SlaBreachedNotification> BreachedNotifications { get; set; } = [];

    public IReadOnlyList<SlaCanceledNotification> CanceledNotifications { get; set; } = [];

    public IReadOnlyList<AdminReassignmentNotification> AdminReassignmentNotifications { get; set; } = [];

    public List<Guid> MarkedExpiring { get; } = [];

    public List<Guid> MarkedBreached { get; } = [];

    public List<Guid> MarkedCanceled { get; } = [];

    public List<Guid> MarkedAdminReassignment { get; } = [];

    public int GraceExpirationsApplied { get; private set; }

    public int AssignmentStartGraceExpirationsApplied { get; private set; }

    public bool ThrowOnExpiring { get; set; }

    public bool ThrowOnBreached { get; set; }

    public bool ThrowOnCanceled { get; set; }

    public bool ThrowOnAdminReassignment { get; set; }

    public Task<IReadOnlyList<SlaExpiringNotification>> GetTicketsPendingExpiringNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnExpiring)
        {
            throw new InvalidOperationException("Fallo simulado al detectar SLA por vencer.");
        }

        return Task.FromResult(ExpiringNotifications);
    }

    public Task MarkExpiringNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default)
    {
        MarkedExpiring.AddRange(ticketIds);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SlaBreachedNotification>> GetTicketsPendingBreachNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnBreached)
        {
            throw new InvalidOperationException("Fallo simulado al detectar SLA incumplido.");
        }

        return Task.FromResult(BreachedNotifications);
    }

    public Task MarkBreachedNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default)
    {
        MarkedBreached.AddRange(ticketIds);

        return Task.CompletedTask;
    }

    public Task ApplySlaGraceExpirationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        GraceExpirationsApplied++;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SlaCanceledNotification>> GetPendingSlaCanceledNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnCanceled)
        {
            throw new InvalidOperationException("Fallo simulado al detectar participaciones canceladas.");
        }

        return Task.FromResult(CanceledNotifications);
    }

    public Task MarkSlaCanceledNotifiedAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default)
    {
        MarkedCanceled.AddRange(recordIds);

        return Task.CompletedTask;
    }

    public Task ApplyAssignmentStartGraceExpirationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        AssignmentStartGraceExpirationsApplied++;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AdminReassignmentNotification>> GetPendingAdminReassignmentNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnAdminReassignment)
        {
            throw new InvalidOperationException("Fallo simulado al detectar reasignaciones pendientes.");
        }

        return Task.FromResult(AdminReassignmentNotifications);
    }

    public Task MarkAdminReassignmentNotifiedAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default)
    {
        MarkedAdminReassignment.AddRange(recordIds);

        return Task.CompletedTask;
    }
}