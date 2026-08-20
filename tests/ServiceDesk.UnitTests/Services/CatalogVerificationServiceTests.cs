using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Features.Catalog;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Services;

public sealed class CatalogVerificationServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public async Task EnsureCategoryBelongsToCompanyAsync_OwnActiveCategory_DoesNotThrow()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Category category = CreateCategory(CompanyId, isActive: true);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        await service.EnsureCategoryBelongsToCompanyAsync(category.Id);
    }

    [Fact]
    public async Task EnsureCategoryBelongsToCompanyAsync_CategoryOfAnotherCompany_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Category category = CreateCategory(Guid.NewGuid());
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsureCategoryBelongsToCompanyAsync(category.Id));

        Assert.Contains("CategoryId", exception.Errors.Keys);
    }

    [Fact]
    public async Task EnsureCategoryBelongsToCompanyAsync_InactiveCategory_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Category category = CreateCategory(CompanyId, isActive: false);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsureCategoryBelongsToCompanyAsync(category.Id));
    }

    [Fact]
    public async Task EnsureCategoryBelongsToCompanyAsync_NonExistentCategory_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();

        ICatalogVerificationService service = CreateService(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsureCategoryBelongsToCompanyAsync(Guid.NewGuid()));
    }

    private static ServiceDeskDbContext CreateContext()
    {
        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ServiceDeskDbContext(options);
    }

    private static CatalogVerificationService CreateService(ServiceDeskDbContext context) =>
        new(new CatalogRepository(context), new FakeCurrentUserService(Guid.NewGuid(), CompanyId));

    private static Category CreateCategory(Guid companyId, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Hardware", IsActive = isActive };
}
