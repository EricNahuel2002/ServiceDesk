using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ISlaMonitoringService
{
    Task<IReadOnlyList<SlaExpiringNotification>> GetTicketsPendingExpiringNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task MarkExpiringNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlaBreachedNotification>> GetTicketsPendingBreachNotificationAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task MarkBreachedNotifiedAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default);

    Task ApplySlaGraceExpirationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task ApplyAssignmentStartGraceExpirationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminReassignmentNotification>> GetPendingAdminReassignmentNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task MarkAdminReassignmentNotifiedAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlaCanceledNotification>> GetPendingSlaCanceledNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task MarkSlaCanceledNotifiedAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default);
}