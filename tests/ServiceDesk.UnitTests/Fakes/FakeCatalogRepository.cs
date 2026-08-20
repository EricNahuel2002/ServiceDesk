using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeCatalogRepository : ICatalogRepository
{
    public Guid? NuevoStatusId { get; set; }

    public Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Guid?> FindInitialStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(NuevoStatusId);

    public Task<Guid?> FindFirstClosedStatusIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Guid?> FindStatusByNameAsync(
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NuevoStatusId);

    public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> CategoryNameExistsAsync(
        Guid companyId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}