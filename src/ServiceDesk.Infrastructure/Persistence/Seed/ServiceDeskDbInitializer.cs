using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Infrastructure.Persistence.Seed;

public sealed class ServiceDeskDbInitializer
{
    private readonly ServiceDeskDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<ServiceDeskDbInitializer> _logger;

    public ServiceDeskDbInitializer(
        ServiceDeskDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<ServiceDeskDbInitializer> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        if (await _context.Companies.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedCompanyAsync(cancellationToken);
        await SeedDemoAdminAsync(cancellationToken);

        _logger.LogInformation("Datos demo sembrados correctamente.");
    }

    private async Task SeedCompanyAsync(CancellationToken cancellationToken)
    {
        Company company = new()
        {
            Id = Guid.NewGuid(),
            Name = "Contoso S.A.",
            IsActive = true
        };

        _context.Companies.Add(company);

        _context.Categories.AddRange(
            CreateCategory(company, "Hardware"),
            CreateCategory(company, "Software"),
            CreateCategory(company, "Red"),
            CreateCategory(company, "Acceso y Cuentas"));

        _context.Priorities.AddRange(
            CreatePriority(company, "Baja", 1),
            CreatePriority(company, "Media", 2),
            CreatePriority(company, "Alta", 3),
            CreatePriority(company, "Urgente", 4));

        _context.Statuses.AddRange(
            CreateStatus(company, "Nuevo", 1),
            CreateStatus(company, "En Progreso", 2),
            CreateStatus(company, "En Espera", 3),
            CreateStatus(company, "Resuelto", 4, isClosed: true),
            CreateStatus(company, "Cerrado", 5, isClosed: true));

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoAdminAsync(CancellationToken cancellationToken)
    {
        if (await _roleManager.FindByNameAsync("Administrator") is null)
        {
            throw new InvalidOperationException("El rol Administrator debe existir antes de sembrar el usuario admin.");
        }

        Company company = await _context.Companies
            .FirstAsync(c => c.Name == "Contoso S.A.", cancellationToken);

        var admin = new ApplicationUser
        {
            UserName = "admin@servicedesk.local",
            Email = "admin@servicedesk.local",
            FirstName = "Admin",
            LastName = "ServiceDesk",
            CompanyId = company.Id,
            EmailConfirmed = true,
            IsActive = true
        };

        IdentityResult result = await _userManager.CreateAsync(admin, "Admin*123456");

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear el usuario admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(admin, "Administrator");

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo asignar el rol al usuario admin: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    private static Category CreateCategory(Company company, string name) =>
        new() { CompanyId = company.Id, Name = name, IsActive = true };

    private static Priority CreatePriority(Company company, string name, int sortOrder) =>
        new() { CompanyId = company.Id, Name = name, SortOrder = sortOrder, IsActive = true };

    private static Status CreateStatus(Company company, string name, int sortOrder, bool isClosed = false) =>
        new() { CompanyId = company.Id, Name = name, SortOrder = sortOrder, IsClosed = isClosed, IsActive = true };
}
