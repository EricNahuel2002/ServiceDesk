namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record SlaBreachedNotification
{
    public Guid TicketId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string PriorityName { get; init; } = string.Empty;

    public string TechnicianFirstName { get; init; } = string.Empty;

    public string TechnicianEmail { get; init; } = string.Empty;

    public DateTime ResponseDeadlineAtUtc { get; init; }

    public DateTime GraceDeadlineUtc { get; init; }
}