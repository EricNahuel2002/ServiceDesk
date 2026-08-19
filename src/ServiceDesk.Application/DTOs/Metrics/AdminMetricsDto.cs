using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Metrics;

public sealed record AdminMetricsDto
{
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public int InProgressTickets { get; init; }
    public int ResolvedTickets { get; init; }
    public int OverdueTickets { get; init; }
    public decimal AverageResolutionHours { get; init; }
    public decimal AverageStartHours { get; init; }
    public decimal SlaCompliancePercentage { get; init; }
    public IReadOnlyList<PriorityMetricDto> ByPriority { get; init; } = [];
    public IReadOnlyList<DailyMetricDto> DailyTrend { get; init; } = [];
    public IReadOnlyList<TechnicianMetricDto> ByTechnician { get; init; } = [];
}

public sealed record PriorityMetricDto
{
    public TicketPriority? Priority { get; init; }
    public int Count { get; init; }
    public int OverdueCount { get; init; }
}

public sealed record DailyMetricDto
{
    public DateOnly Date { get; init; }
    public int Created { get; init; }
    public int Resolved { get; init; }
}

public sealed record TechnicianMetricDto
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int AssignedCount { get; init; }
    public int ResolvedCount { get; init; }
    public decimal AverageResolutionHours { get; init; }
    public decimal AverageStartHours { get; init; }
}
