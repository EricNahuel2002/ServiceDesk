namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record TicketAssignedNotification
{
    public string EventType { get; init; } = "TicketAssigned";

    public Guid TicketId { get; init; }
}
