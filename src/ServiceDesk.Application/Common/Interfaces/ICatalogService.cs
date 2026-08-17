using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriorityDto>> GetAllPrioritiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<PriorityDto> CreatePriorityAsync(CreatePriorityRequest request, CancellationToken cancellationToken = default);

    Task<PriorityDto> UpdatePriorityAsync(Guid id, UpdatePriorityRequest request, CancellationToken cancellationToken = default);

    Task<StatusDto> CreateStatusAsync(CreateStatusRequest request, CancellationToken cancellationToken = default);

    Task<StatusDto> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken cancellationToken = default);
}
