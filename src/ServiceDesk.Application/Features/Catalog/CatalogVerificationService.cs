using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Features.Catalog;

public sealed class CatalogVerificationService : ICatalogVerificationService
{
    private readonly ICatalogRepository _catalog;
    private readonly ICurrentUserService _currentUser;

    public CatalogVerificationService(ICatalogRepository catalog, ICurrentUserService currentUser)
    {
        _catalog = catalog;
        _currentUser = currentUser;
    }

    public async Task EnsureCategoryBelongsToCompanyAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        Category? category = await _catalog.GetCategoryByIdAsync(categoryId, cancellationToken);

        if (category is null || !category.IsActive || category.CompanyId != _currentUser.CompanyId)
        {
            ThrowInvalidCatalogItem("CategoryId");
        }
    }

    private static void ThrowInvalidCatalogItem(string propertyName) =>
        throw new ValidationException(new Dictionary<string, string[]>
        {
            [propertyName] = ["El catálogo indicado no existe, no pertenece a tu empresa o está inactivo."]
        });
}
