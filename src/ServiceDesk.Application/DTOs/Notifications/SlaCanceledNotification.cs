namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record SlaCanceledNotification
{
    public Guid RecordId { get; init; }

    public Guid TicketId { get; init; }

    public Guid TechnicianId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string PriorityName { get; init; } = string.Empty;

    public string TechnicianFirstName { get; init; } = string.Empty;

    public string TechnicianEmail { get; init; } = string.Empty;

    public DateTime CanceledAtUtc { get; init; }
}