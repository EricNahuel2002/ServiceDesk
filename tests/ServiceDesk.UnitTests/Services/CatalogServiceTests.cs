using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Application.Features.Catalog;
using ServiceDesk.Application.Features.Catalog.Validators;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Services;

public sealed class CatalogServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public async Task GetCategoriesAsync_ReturnsOnlyActiveCategoriesOfCurrentCompany()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Guid otherCompanyId = Guid.NewGuid();

        context.Categories.AddRange(
            CreateCategory(CompanyId, "Hardware"),
            CreateCategory(CompanyId, "Software", isActive: false),
            CreateCategory(otherCompanyId, "Red"));

        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<CategoryDto> categories = await service.GetCategoriesAsync();

        CategoryDto category = Assert.Single(categories);
        Assert.Equal("Hardware", category.Name);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsEmpty_WhenCompanyHasNoActiveCategories()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Categories.Add(CreateCategory(CompanyId, "Software", isActive: false));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<CategoryDto> categories = await service.GetCategoriesAsync();

        Assert.Empty(categories);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesOrderedByName()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Categories.AddRange(
            CreateCategory(CompanyId, "Zebra"),
            CreateCategory(CompanyId, "Alpha"),
            CreateCategory(CompanyId, "Mid"));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<CategoryDto> categories = await service.GetCategoriesAsync();

        Assert.Equal(["Alpha", "Mid", "Zebra"], categories.Select(category => category.Name));
    }

    private static ServiceDeskDbContext CreateContext()
    {
        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ServiceDeskDbContext(options);
    }

    private static CatalogService CreateService(ServiceDeskDbContext context) =>
        new(
            new CatalogRepository(context),
            new FakeCurrentUserService(Guid.NewGuid(), CompanyId),
            context,
            new CreateCategoryRequestValidator(),
            new UpdateCategoryRequestValidator());

    private static Category CreateCategory(Guid companyId, string name, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = name, IsActive = isActive };
}
