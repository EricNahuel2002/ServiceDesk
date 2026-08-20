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

    public async Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .Where(status => status.CompanyId == companyId && status.Name == "Nuevo" && status.IsActive)
            .Select(status => (Guid?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Guid?> FindFirstClosedStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .Where(status => status.CompanyId == companyId && status.IsActive && status.IsClosed)
            .OrderBy(status => status.SortOrder)
            .Select(status => (Guid?)status.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .Where(category => category.CompanyId == companyId)
            .OrderBy(category => category.Name)
            .Select(CategoryProjection)
            .ToListAsync(cancellationToken);

    public async Task<Guid?> FindStatusByNameAsync(
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default) =>
        await _context.Statuses
            .AsNoTracking()
            .Where(status => status.CompanyId == companyId && status.Name == name && status.IsActive)
            .Select(status => (Guid?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public async Task<bool> CategoryNameExistsAsync(
        Guid companyId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AnyAsync(
                c => c.CompanyId == companyId && c.Name == name && (excludeId == null || c.Id != excludeId.Value),
                cancellationToken);
}
