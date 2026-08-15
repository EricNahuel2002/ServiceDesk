namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record TicketNotificationInfo
{
    public Guid TicketId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string PriorityName { get; init; } = string.Empty;

    public string AssignedToFirstName { get; init; } = string.Empty;

    public string AssignedToLastName { get; init; } = string.Empty;

    public string AssignedToEmail { get; init; } = string.Empty;

    public string RequesterFirstName { get; init; } = string.Empty;

    public string RequesterEmail { get; init; } = string.Empty;
}
