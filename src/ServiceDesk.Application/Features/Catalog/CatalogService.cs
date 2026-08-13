using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Application.Features.Catalog;

public sealed class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalog;
    private readonly ICurrentUserService _currentUser;

    public CatalogService(ICatalogRepository catalog, ICurrentUserService currentUser)
    {
        _catalog = catalog;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveCategoriesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActivePrioritiesAsync(_currentUser.CompanyId, cancellationToken);

    public Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken) =>
        _catalog.GetActiveStatusesAsync(_currentUser.CompanyId, cancellationToken);
}
