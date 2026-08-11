using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.Infrastructure.Persistence;

namespace ServiceDesk.IntegrationTests.Database;

[Collection(IntegrationCollection.Name)]
public sealed class DbConnectionTests
{
    private readonly CustomWebApplicationFactory _factory;

    public DbConnectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CanConnect_ToTestDatabase()
    {
        var (scope, context) = CreateScopedContext();
        using (scope)
        {
            Assert.True(await context.Database.CanConnectAsync());
        }
    }

    [Fact]
    public async Task Migrations_AreAllApplied_AfterHostStartup()
    {
        var (scope, context) = CreateScopedContext();
        using (scope)
        {
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
    }

    [Fact]
    public async Task Seed_CreatesCompanyRolesUsersAndCatalog()
    {
        var (scope, context) = CreateScopedContext();
        using (scope)
        {
            Assert.True(
                await context.Companies.AnyAsync(c => c.Name == CustomWebApplicationFactory.SeedCompanyName),
                "No se encontró la empresa semilla.");

            Assert.True(await context.Users.AnyAsync(u => u.Email == CustomWebApplicationFactory.AdminEmail));
            Assert.True(await context.Users.AnyAsync(u => u.Email == CustomWebApplicationFactory.TechnicianEmail));
            Assert.True(await context.Users.AnyAsync(u => u.Email == CustomWebApplicationFactory.ClientEmail));

            foreach (string role in Roles.All)
            {
                Assert.True(
                    await context.Roles.AnyAsync(r => r.Name == role),
                    $"Falta el rol '{role}'.");
            }

            Assert.True(await context.Categories.AnyAsync());
            Assert.True(await context.Priorities.AnyAsync());
            Assert.True(await context.Statuses.AnyAsync());
        }
    }

    [Fact]
    public async Task Ticket_InsertUpdateDelete_Roundtrips()
    {
        await _factory.ResetTicketsAsync();

        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid priorityId = await _factory.GetPriorityIdAsync(companyId, "Media");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        Guid createdBy = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.AdminEmail);

        var (scope, context) = CreateScopedContext();
        using (scope)
        {
            Ticket ticket = new()
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CategoryId = categoryId,
                PriorityId = priorityId,
                StatusId = statusId,
                CreatedById = createdBy,
                Title = "Roundtrip",
                Description = "Descripción del roundtrip"
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            Ticket? loaded = await context.Tickets
                .SingleOrDefaultAsync(t => t.Id == ticket.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Roundtrip", loaded.Title);

            ticket.Title = "Roundtrip actualizado";
            await context.SaveChangesAsync();

            Ticket? updated = await context.Tickets.AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == ticket.Id);
            Assert.NotNull(updated);
            Assert.Equal("Roundtrip actualizado", updated.Title);

            context.Tickets.Remove(ticket);
            await context.SaveChangesAsync();

            Assert.False(await context.Tickets.AnyAsync(t => t.Id == ticket.Id));
        }
    }

    private (IServiceScope Scope, ServiceDeskDbContext Context) CreateScopedContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return (scope, context);
    }
}
