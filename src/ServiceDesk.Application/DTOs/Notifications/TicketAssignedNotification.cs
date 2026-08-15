namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record TicketAssignedNotification
{
    public string EventType { get; init; } = NotificationEvents.TicketAssigned;

    public Guid TicketId { get; init; }
}
