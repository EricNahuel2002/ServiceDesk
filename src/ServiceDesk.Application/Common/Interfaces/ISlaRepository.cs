using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ISlaRepository
{
    Task<IReadOnlyList<SlaConfiguration>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<SlaConfiguration?> FindByCompanyAndPriorityAsync(
        Guid companyId,
        Domain.Enums.TicketPriority priority,
        CancellationToken cancellationToken = default);

    Task<CompanyBusinessHours?> GetBusinessHoursAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<SlaConfiguration> configurations, CancellationToken cancellationToken = default);

    Task AddAsync(CompanyBusinessHours businessHours, CancellationToken cancellationToken = default);
}
