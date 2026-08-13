using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class CatalogRepository : ICatalogRepository
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

    private readonly ServiceDeskDbContext _context;

    public CatalogRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .Where(category => category.CompanyId == companyId && category.IsActive)
            .OrderBy(category => category.Name)
            .Select(CategoryProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PriorityDto>> GetActivePrioritiesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.Priorities
            .AsNoTracking()
            .Where(priority => priority.CompanyId == companyId && priority.IsActive)
            .OrderBy(priority => priority.SortOrder)
            .Select(PriorityProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StatusDto>> GetActiveStatusesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .AsNoTracking()
            .Where(status => status.CompanyId == companyId && status.IsActive)
            .OrderBy(status => status.SortOrder)
            .Select(StatusProjection)
            .ToListAsync(cancellationToken);

    public async Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Priority?> GetPriorityByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Priorities
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Status?> GetStatusByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .Where(status => status.CompanyId == companyId && status.Name == "Nuevo" && status.IsActive)
            .Select(status => (Guid?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Guid?> FindDefaultPriorityIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Priorities
            .Where(priority => priority.CompanyId == companyId && priority.Name == "Media" && priority.IsActive)
            .Select(priority => (Guid?)priority.Id)
            .SingleOrDefaultAsync(cancellationToken);
}
