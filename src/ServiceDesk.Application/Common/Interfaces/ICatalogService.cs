using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
}
