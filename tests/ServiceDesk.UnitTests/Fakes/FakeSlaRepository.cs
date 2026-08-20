using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeSlaRepository : ISlaRepository
{
    public CompanyBusinessHours? BusinessHours { get; set; }

    public Task<IReadOnlyList<SlaConfiguration>> GetByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<SlaConfiguration?> FindByCompanyAndPriorityAsync(
        Guid companyId,
        TicketPriority priority,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CompanyBusinessHours?> GetBusinessHoursAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(BusinessHours);

    public Task AddRangeAsync(
        IEnumerable<SlaConfiguration> configurations,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddAsync(
        CompanyBusinessHours businessHours,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}