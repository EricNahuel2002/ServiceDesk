using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Features.Catalog;

public sealed class CatalogVerificationService : ICatalogVerificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CatalogVerificationService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task EnsureCategoryBelongsToCompanyAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        Category? category = await _context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);

        if (!IsAvailableForCompany(category, _currentUser.CompanyId))
        {
            ThrowInvalidCatalogItem("CategoryId");
        }
    }

    public async Task EnsurePriorityBelongsToCompanyAsync(
        Guid priorityId,
        CancellationToken cancellationToken)
    {
        Priority? priority = await _context.Priorities
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == priorityId, cancellationToken);

        if (!IsAvailableForCompany(priority, _currentUser.CompanyId))
        {
            ThrowInvalidCatalogItem("PriorityId");
        }
    }

    public async Task<Status> EnsureStatusBelongsToCompanyAsync(
        Guid statusId,
        CancellationToken cancellationToken)
    {
        Status? status = await _context.Statuses
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == statusId, cancellationToken);

        if (!IsAvailableForCompany(status, _currentUser.CompanyId))
        {
            ThrowInvalidCatalogItem("StatusId");
        }

        return status!;
    }

    private static bool IsAvailableForCompany(Category? item, Guid companyId) =>
        item is not null
        && item.IsActive
        && CompanyCatalogPolicy.BelongsToCompany(item, companyId);

    private static bool IsAvailableForCompany(Priority? item, Guid companyId) =>
        item is not null
        && item.IsActive
        && CompanyCatalogPolicy.BelongsToCompany(item, companyId);

    private static bool IsAvailableForCompany(Status? item, Guid companyId) =>
        item is not null
        && item.IsActive
        && CompanyCatalogPolicy.BelongsToCompany(item, companyId);

    private static void ThrowInvalidCatalogItem(string propertyName) =>
        throw new ValidationException(new Dictionary<string, string[]>
        {
            [propertyName] = ["El catálogo indicado no existe, no pertenece a tu empresa o está inactivo."]
        });
}
