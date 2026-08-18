using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetActiveStatusesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetAllStatusesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Status?> GetStatusByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid?> FindFirstClosedStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);

    Task AddStatusAsync(Status status, CancellationToken cancellationToken = default);

    Task<bool> CategoryNameExistsAsync(Guid companyId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> StatusNameExistsAsync(Guid companyId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
