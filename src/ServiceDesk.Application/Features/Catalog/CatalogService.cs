using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Features.Catalog;

public sealed class CatalogService : ICatalogService
{
    private static readonly Expression<Func<Category, CategoryDto>> CategoryProjection = category => new CategoryDto
    {
        Id = category.Id,
        Name = category.Name,
        IsActive = category.IsActive
    };

    private static readonly Expression<Func<Priority, PriorityDto>> PriorityProjection = priority => new PriorityDto
    {
        Id = priority.Id,
        Name = priority.Name,
        SortOrder = priority.SortOrder,
        IsActive = priority.IsActive
    };

    private static readonly Expression<Func<Status, StatusDto>> StatusProjection = status => new StatusDto
    {
        Id = status.Id,
        Name = status.Name,
        SortOrder = status.SortOrder,
        IsClosed = status.IsClosed,
        IsActive = status.IsActive
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CatalogService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        List<CategoryDto> categories = await _context.Categories
            .AsNoTracking()
            .Where(category => category.CompanyId == _currentUser.CompanyId && category.IsActive)
            .OrderBy(category => category.Name)
            .Select(CategoryProjection)
            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken cancellationToken)
    {
        List<PriorityDto> priorities = await _context.Priorities
            .AsNoTracking()
            .Where(priority => priority.CompanyId == _currentUser.CompanyId && priority.IsActive)
            .OrderBy(priority => priority.SortOrder)
            .Select(PriorityProjection)
            .ToListAsync(cancellationToken);

        return priorities;
    }

    public async Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        List<StatusDto> statuses = await _context.Statuses
            .AsNoTracking()
            .Where(status => status.CompanyId == _currentUser.CompanyId && status.IsActive)
            .OrderBy(status => status.SortOrder)
            .Select(StatusProjection)
            .ToListAsync(cancellationToken);

        return statuses;
    }
}
