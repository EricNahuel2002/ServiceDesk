using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Features.Catalog;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Infrastructure.Persistence;
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

    [Fact]
    public async Task EnsurePriorityBelongsToCompanyAsync_OwnActivePriority_DoesNotThrow()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Priority priority = CreatePriority(CompanyId, isActive: true);
        context.Priorities.Add(priority);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        await service.EnsurePriorityBelongsToCompanyAsync(priority.Id);
    }

    [Fact]
    public async Task EnsurePriorityBelongsToCompanyAsync_PriorityOfAnotherCompany_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Priority priority = CreatePriority(Guid.NewGuid());
        context.Priorities.Add(priority);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsurePriorityBelongsToCompanyAsync(priority.Id));

        Assert.Contains("PriorityId", exception.Errors.Keys);
    }

    [Fact]
    public async Task EnsurePriorityBelongsToCompanyAsync_InactivePriority_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Priority priority = CreatePriority(CompanyId, isActive: false);
        context.Priorities.Add(priority);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsurePriorityBelongsToCompanyAsync(priority.Id));
    }

    [Fact]
    public async Task EnsureStatusBelongsToCompanyAsync_OwnActiveStatus_ReturnsStatus()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Status status = CreateStatus(CompanyId, isActive: true);
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        Status validated = await service.EnsureStatusBelongsToCompanyAsync(status.Id);

        Assert.Equal(status.Id, validated.Id);
    }

    [Fact]
    public async Task EnsureStatusBelongsToCompanyAsync_StatusOfAnotherCompany_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Status status = CreateStatus(Guid.NewGuid());
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsureStatusBelongsToCompanyAsync(status.Id));

        Assert.Contains("StatusId", exception.Errors.Keys);
    }

    [Fact]
    public async Task EnsureStatusBelongsToCompanyAsync_InactiveStatus_Throws()
    {
        await using ServiceDeskDbContext context = CreateContext();
        Status status = CreateStatus(CompanyId, isActive: false);
        context.Statuses.Add(status);
        await context.SaveChangesAsync();

        ICatalogVerificationService service = CreateService(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.EnsureStatusBelongsToCompanyAsync(status.Id));
    }

    private static ServiceDeskDbContext CreateContext()
    {
        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ServiceDeskDbContext(options);
    }

    private static CatalogVerificationService CreateService(ServiceDeskDbContext context) =>
        new(context, new FakeCurrentUserService(Guid.NewGuid(), CompanyId));

    private static Category CreateCategory(Guid companyId, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Hardware", IsActive = isActive };

    private static Priority CreatePriority(Guid companyId, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Media", SortOrder = 2, IsActive = isActive };

    private static Status CreateStatus(Guid companyId, bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = "Nuevo",
            SortOrder = 1,
            IsActive = isActive,
            IsClosed = false
        };
}
