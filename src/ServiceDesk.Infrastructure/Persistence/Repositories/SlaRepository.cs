using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class SlaRepository : ISlaRepository
{
    private readonly ServiceDeskDbContext _context;

    public SlaRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SlaConfiguration>> GetByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.SlaConfigurations
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Priority)
            .ToListAsync(cancellationToken);

    public async Task<SlaConfiguration?> FindByCompanyAndPriorityAsync(
        Guid companyId,
        TicketPriority priority,
        CancellationToken cancellationToken = default) =>
        await _context.SlaConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.CompanyId == companyId && s.Priority == priority,
                cancellationToken);

    public async Task<CompanyBusinessHours?> GetBusinessHoursAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.CompanyBusinessHours
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.CompanyId == companyId, cancellationToken);

    public async Task AddRangeAsync(
        IEnumerable<SlaConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        await _context.SlaConfigurations.AddRangeAsync(configurations, cancellationToken);

    public async Task AddAsync(
        CompanyBusinessHours businessHours,
        CancellationToken cancellationToken = default) =>
        await _context.CompanyBusinessHours.AddAsync(businessHours, cancellationToken);
}
