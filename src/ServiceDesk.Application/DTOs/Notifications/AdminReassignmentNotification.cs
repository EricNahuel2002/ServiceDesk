using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.DTOs.Notifications;

public sealed record AdminReassignmentNotification
{
    public Guid RecordId { get; init; }

    public Guid TicketId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string PriorityName { get; init; } = string.Empty;

    public SlaRecordCancelReason CancelReason { get; init; }

    public string TechnicianFirstName { get; init; } = string.Empty;

    public string TechnicianLastName { get; init; } = string.Empty;

    public DateTime AssignedAtUtc { get; init; }

    public DateTime? StartGraceDeadlineUtc { get; init; }

    public IReadOnlyList<string> AdminEmails { get; init; } = [];
}
