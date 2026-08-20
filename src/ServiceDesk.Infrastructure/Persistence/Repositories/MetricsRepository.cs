using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Metrics;
using ServiceDesk.Infrastructure.Persistence;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class MetricsRepository : IMetricsRepository
{
    private readonly ServiceDeskDbContext _context;

    public MetricsRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketMetricsRecord>> GetTicketMetricsAsync(
        Guid companyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Tickets.Ticket> query = _context.Tickets
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Include(t => t.Status)
            .Include(t => t.AssignedTo);

        if (from.HasValue)
        {
            DateTime fromUtc = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(t => t.CreatedAtUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            DateTime toUtc = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(t => t.CreatedAtUtc < toUtc);
        }

        List<Domain.Tickets.Ticket> tickets = await query.ToListAsync(cancellationToken);

        return tickets.Select(t => new TicketMetricsRecord
        {
            Id = t.Id,
            Priority = t.Priority,
            StatusId = t.StatusId,
            StatusIsClosed = t.Status?.IsClosed ?? false,
            StatusName = t.Status?.Name ?? string.Empty,
            CreatedAtUtc = t.CreatedAtUtc,
            StartedWorkAtUtc = t.StartedWorkAtUtc,
            ResolvedAtUtc = t.ResolvedAtUtc,
            ResponseDeadlineAtUtc = t.ResponseDeadlineAtUtc,
            AssignedToId = t.AssignedToId,
            AssignedToFirstName = t.AssignedTo?.FirstName,
            AssignedToLastName = t.AssignedTo?.LastName
        }).ToList();
    }
}
