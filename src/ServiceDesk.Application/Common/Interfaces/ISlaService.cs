using ServiceDesk.Application.DTOs.Sla;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ISlaService
{
    Task<IReadOnlyList<SlaConfigurationDto>> GetSlaConfigurationsAsync(CancellationToken cancellationToken = default);

    Task<SlaConfigurationDto> UpdateSlaConfigurationAsync(
        UpdateSlaConfigurationRequest request,
        CancellationToken cancellationToken = default);

    Task<BusinessHoursDto> GetBusinessHoursAsync(CancellationToken cancellationToken = default);

    Task<BusinessHoursDto> UpdateBusinessHoursAsync(
        UpdateBusinessHoursRequest request,
        CancellationToken cancellationToken = default);
}
