using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Metrics;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeMetricsRepository(List<TicketMetricsRecord> tickets) : IMetricsRepository
{
    public List<TicketMetricsRecord> Tickets { get; } = tickets;

    public Task<IReadOnlyList<TicketMetricsRecord>> GetTicketMetricsAsync(
        Guid companyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TicketMetricsRecord> query = Tickets;

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

        return Task.FromResult<IReadOnlyList<TicketMetricsRecord>>(query.ToList());
    }
}
