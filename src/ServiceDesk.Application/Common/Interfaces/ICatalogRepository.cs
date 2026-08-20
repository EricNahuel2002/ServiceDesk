using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid?> FindFirstClosedStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid?> FindStatusByNameAsync(Guid companyId, string name, CancellationToken cancellationToken = default);

    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> CategoryNameExistsAsync(Guid companyId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
