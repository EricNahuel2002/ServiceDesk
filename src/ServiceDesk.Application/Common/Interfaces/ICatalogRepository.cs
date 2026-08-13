using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriorityDto>> GetActivePrioritiesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetActiveStatusesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Priority?> GetPriorityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Status?> GetStatusByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid?> FindDefaultPriorityIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
