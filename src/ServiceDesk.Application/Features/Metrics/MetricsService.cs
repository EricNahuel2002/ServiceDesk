using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Metrics;

namespace ServiceDesk.Application.Features.Metrics;

public sealed class MetricsService : IMetricsService
{
    private readonly IMetricsRepository _metricsRepository;
    private readonly ICurrentUserService _currentUser;

    public MetricsService(IMetricsRepository metricsRepository, ICurrentUserService currentUser)
    {
        _metricsRepository = metricsRepository;
        _currentUser = currentUser;
    }

    public async Task<AdminMetricsDto> GetAdminMetricsAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? technicianId,
        string? period,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketMetricsRecord> tickets = await _metricsRepository.GetTicketMetricsAsync(
            _currentUser.CompanyId,
            from,
            to,
            cancellationToken);

        if (technicianId.HasValue)
        {
            tickets = tickets.Where(t => t.AssignedToId == technicianId.Value).ToList();
        }

        int total = tickets.Count;
        int open = tickets.Count(t => !t.StatusIsClosed && t.StartedWorkAtUtc is null);
        int inProgress = tickets.Count(t => !t.StatusIsClosed && t.StartedWorkAtUtc is not null);
        int resolved = tickets.Count(t => IsResolved(t));
        int overdue = tickets.Count(t => IsOverdue(t));

        decimal avgResolutionHours = ComputeAverage(
            tickets.Where(t => t.ResolvedAtUtc.HasValue && t.StartedWorkAtUtc.HasValue)
                .Select(t => (t.ResolvedAtUtc!.Value - t.StartedWorkAtUtc!.Value).TotalHours));

        decimal avgStartHours = ComputeAverage(
            tickets.Where(t => t.StartedWorkAtUtc.HasValue)
                .Select(t => (t.StartedWorkAtUtc!.Value - t.CreatedAtUtc).TotalHours));

        int slaCompliance = total > 0
            ? (int)Math.Round((decimal)(total - overdue) / total * 100)
            : 100;

        IReadOnlyList<PriorityMetricDto> byPriority = tickets
            .GroupBy(t => t.Priority)
            .Select(g => new PriorityMetricDto
            {
                Priority = g.Key,
                Count = g.Count(),
                OverdueCount = g.Count(IsOverdue)
            })
            .OrderByDescending(x => (int)x.Priority)
            .ToList();

        IReadOnlyList<DailyMetricDto> trend = GroupTrend(tickets, period);

        IReadOnlyList<TechnicianMetricDto> byTechnician = tickets
            .Where(t => t.AssignedToId.HasValue)
            .GroupBy(t => t.AssignedToId!.Value)
            .Select(g =>
            {
                TicketMetricsRecord first = g.First();
                decimal techAvgResolution = ComputeAverage(
                    g.Where(t => t.ResolvedAtUtc.HasValue && t.StartedWorkAtUtc.HasValue)
                        .Select(t => (t.ResolvedAtUtc!.Value - t.StartedWorkAtUtc!.Value).TotalHours));

                decimal techAvgStart = ComputeAverage(
                    g.Where(t => t.StartedWorkAtUtc.HasValue)
                        .Select(t => (t.StartedWorkAtUtc!.Value - t.CreatedAtUtc).TotalHours));

                return new TechnicianMetricDto
                {
                    UserId = g.Key,
                    FirstName = first.AssignedToFirstName ?? string.Empty,
                    LastName = first.AssignedToLastName ?? string.Empty,
                    AssignedCount = g.Count(),
                    ResolvedCount = g.Count(IsResolved),
                    AverageResolutionHours = techAvgResolution,
                    AverageStartHours = techAvgStart
                };
            })
            .OrderByDescending(x => x.AssignedCount)
            .ToList();

        return new AdminMetricsDto
        {
            TotalTickets = total,
            OpenTickets = open,
            InProgressTickets = inProgress,
            ResolvedTickets = resolved,
            OverdueTickets = overdue,
            AverageResolutionHours = Math.Round(avgResolutionHours, 1),
            AverageStartHours = Math.Round(avgStartHours, 1),
            SlaCompliancePercentage = slaCompliance,
            ByPriority = byPriority,
            DailyTrend = trend,
            ByTechnician = byTechnician
        };
    }

    private static IReadOnlyList<DailyMetricDto> GroupTrend(
        IReadOnlyList<TicketMetricsRecord> tickets,
        string? period)
    {
        return period?.ToLowerInvariant() switch
        {
            "week" => tickets
                .GroupBy(t => GetWeekStart(DateOnly.FromDateTime(t.CreatedAtUtc)))
                .Select(g => new DailyMetricDto
                {
                    Date = g.Key,
                    Created = g.Count(),
                    Resolved = g.Count(IsResolved)
                })
                .OrderBy(x => x.Date)
                .ToList(),
            "month" => tickets
                .GroupBy(t => GetMonthStart(DateOnly.FromDateTime(t.CreatedAtUtc)))
                .Select(g => new DailyMetricDto
                {
                    Date = g.Key,
                    Created = g.Count(),
                    Resolved = g.Count(IsResolved)
                })
                .OrderBy(x => x.Date)
                .ToList(),
            _ => tickets
                .GroupBy(t => DateOnly.FromDateTime(t.CreatedAtUtc))
                .Select(g => new DailyMetricDto
                {
                    Date = g.Key,
                    Created = g.Count(),
                    Resolved = g.Count(IsResolved)
                })
                .OrderBy(x => x.Date)
                .ToList()
        };
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    private static DateOnly GetMonthStart(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static decimal ComputeAverage(IEnumerable<double> values)
    {
        List<double> list = values.ToList();
        return list.Count > 0 ? (decimal)list.Average() : 0;
    }

    private static bool IsResolved(TicketMetricsRecord t)
    {
        return t.StatusIsClosed && !IsCancelled(t);
    }

    private static bool IsCancelled(TicketMetricsRecord t)
    {
        return t.StatusName.Contains("Cancelado", StringComparison.OrdinalIgnoreCase)
            || t.StatusName.Contains("Cancelled", StringComparison.OrdinalIgnoreCase)
            || t.StatusName.Contains("Canceled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOverdue(TicketMetricsRecord t)
    {
        if (t.StartedWorkAtUtc is null)
        {
            return false;
        }

        if (t.ResolvedAtUtc is not null)
        {
            return t.ResolvedAtUtc > t.ResponseDeadlineAtUtc;
        }

        return !t.StatusIsClosed && DateTime.UtcNow > t.ResponseDeadlineAtUtc;
    }
}
