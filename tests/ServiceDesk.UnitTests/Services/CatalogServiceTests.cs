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

    [Fact]
    public async Task GetPrioritiesAsync_ReturnsOnlyActivePrioritiesOfCurrentCompany()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Guid otherCompanyId = Guid.NewGuid();

        context.Priorities.AddRange(
            CreatePriority(CompanyId, "Baja", 1),
            CreatePriority(CompanyId, "Alta", 3),
            CreatePriority(CompanyId, "Urgente", 4, isActive: false),
            CreatePriority(otherCompanyId, "Interna", 1));

        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<PriorityDto> priorities = await service.GetPrioritiesAsync();

        Assert.Equal(2, priorities.Count);
        Assert.Equal(["Baja", "Alta"], priorities.Select(priority => priority.Name));
    }

    [Fact]
    public async Task GetPrioritiesAsync_ReturnsEmpty_WhenCompanyHasNoPriorities()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Priorities.Add(CreatePriority(Guid.NewGuid(), "Interna", 1));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<PriorityDto> priorities = await service.GetPrioritiesAsync();

        Assert.Empty(priorities);
    }

    [Fact]
    public async Task GetPrioritiesAsync_ReturnsPrioritiesOrderedBySortOrder()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Priorities.AddRange(
            CreatePriority(CompanyId, "Media", 2),
            CreatePriority(CompanyId, "Urgente", 4),
            CreatePriority(CompanyId, "Baja", 1));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<PriorityDto> priorities = await service.GetPrioritiesAsync();

        Assert.Equal(["Baja", "Media", "Urgente"], priorities.Select(priority => priority.Name));
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsOnlyActiveStatusesOfCurrentCompany()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Guid otherCompanyId = Guid.NewGuid();

        context.Statuses.AddRange(
            CreateStatus(CompanyId, "Nuevo", 1),
            CreateStatus(CompanyId, "En Progreso", 2),
            CreateStatus(CompanyId, "Cerrado", 5, isActive: false),
            CreateStatus(otherCompanyId, "Interno", 1));

        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<StatusDto> statuses = await service.GetStatusesAsync();

        Assert.Equal(2, statuses.Count);
        Assert.Equal(["Nuevo", "En Progreso"], statuses.Select(status => status.Name));
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsEmpty_WhenCompanyHasNoStatuses()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Statuses.Add(CreateStatus(Guid.NewGuid(), "Interno", 1));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<StatusDto> statuses = await service.GetStatusesAsync();

        Assert.Empty(statuses);
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsStatusesOrderedBySortOrder()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Statuses.AddRange(
            CreateStatus(CompanyId, "Resuelto", 4),
            CreateStatus(CompanyId, "Nuevo", 1),
            CreateStatus(CompanyId, "En Espera", 3));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<StatusDto> statuses = await service.GetStatusesAsync();

        Assert.Equal(["Nuevo", "En Espera", "Resuelto"], statuses.Select(status => status.Name));
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsClosedFlag()
    {
        await using ServiceDeskDbContext context = CreateContext();
        context.Statuses.Add(CreateStatus(CompanyId, "Cerrado", 5, isClosed: true));
        await context.SaveChangesAsync();

        ICatalogService service = CreateService(context);

        IReadOnlyList<StatusDto> statuses = await service.GetStatusesAsync();

        StatusDto status = Assert.Single(statuses);
        Assert.True(status.IsClosed);
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
            new UpdateCategoryRequestValidator(),
            new CreatePriorityRequestValidator(),
            new UpdatePriorityRequestValidator(),
            new CreateStatusRequestValidator(),
            new UpdateStatusRequestValidator());

    private static Category CreateCategory(Guid companyId, string name, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = name, IsActive = isActive };

    private static Priority CreatePriority(Guid companyId, string name, int sortOrder, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = name, SortOrder = sortOrder, IsActive = isActive };

    private static Status CreateStatus(
        Guid companyId,
        string name,
        int sortOrder,
        bool isActive = true,
        bool isClosed = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            SortOrder = sortOrder,
            IsActive = isActive,
            IsClosed = isClosed
        };
}
