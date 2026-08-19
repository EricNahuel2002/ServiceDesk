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

        decimal avgResolutionHours = 0;
        if (resolved > 0)
        {
            List<double> resolutionTimes = tickets
                .Where(t => t.ResolvedAtUtc.HasValue && t.StartedWorkAtUtc.HasValue)
                .Select(t => (t.ResolvedAtUtc!.Value - t.StartedWorkAtUtc!.Value).TotalHours)
                .ToList();

            if (resolutionTimes.Count > 0)
            {
                avgResolutionHours = (decimal)resolutionTimes.Average();
            }
        }

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

        List<DailyMetricDto> dailyTrend = tickets
            .GroupBy(t => DateOnly.FromDateTime(t.CreatedAtUtc))
            .Select(g => new DailyMetricDto
            {
                Date = g.Key,
                Created = g.Count(),
                Resolved = g.Count(IsResolved)
            })
            .OrderBy(x => x.Date)
            .ToList();

        IReadOnlyList<TechnicianMetricDto> byTechnician = tickets
            .Where(t => t.AssignedToId.HasValue)
            .GroupBy(t => t.AssignedToId!.Value)
            .Select(g =>
            {
                TicketMetricsRecord first = g.First();
                List<double> resolvedTimes = g
                    .Where(t => t.ResolvedAtUtc.HasValue && t.StartedWorkAtUtc.HasValue)
                    .Select(t => (t.ResolvedAtUtc!.Value - t.StartedWorkAtUtc!.Value).TotalHours)
                    .ToList();

                return new TechnicianMetricDto
                {
                    UserId = g.Key,
                    FirstName = first.AssignedToFirstName ?? string.Empty,
                    LastName = first.AssignedToLastName ?? string.Empty,
                    AssignedCount = g.Count(),
                    ResolvedCount = g.Count(IsResolved),
                    AverageResolutionHours = resolvedTimes.Count > 0
                        ? (decimal)resolvedTimes.Average()
                        : 0
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
            SlaCompliancePercentage = slaCompliance,
            ByPriority = byPriority,
            DailyTrend = dailyTrend,
            ByTechnician = byTechnician
        };
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
