using ServiceDesk.Domain.Audit;

namespace ServiceDesk.Application.DTOs.Audits;

public sealed record TicketAuditEventDto
{
    public DateTime OccurredAtUtc { get; init; }

    public string Action { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? Details { get; init; }

    public string ActorName { get; init; } = string.Empty;
}
