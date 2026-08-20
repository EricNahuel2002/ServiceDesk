using ServiceDesk.Application.DTOs.Metrics;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IMetricsService
{
    Task<AdminMetricsDto> GetAdminMetricsAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? technicianId,
        string? period,
        CancellationToken cancellationToken = default);
}
