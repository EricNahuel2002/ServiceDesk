using ServiceDesk.Application.DTOs.Metrics;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IMetricsRepository
{
    Task<IReadOnlyList<TicketMetricsRecord>> GetTicketMetricsAsync(
        Guid companyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}

public sealed record TicketMetricsRecord
{
    public Guid Id { get; init; }
    public TicketPriority? Priority { get; init; }
    public Guid StatusId { get; init; }
    public bool StatusIsClosed { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? StartedWorkAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public DateTime ResponseDeadlineAtUtc { get; init; }
    public Guid? AssignedToId { get; init; }
    public string? AssignedToFirstName { get; init; }
    public string? AssignedToLastName { get; init; }
}
