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

        await SeedRolesAsync(cancellationToken);

        if (!await _context.Companies.AnyAsync(cancellationToken))
        {
            await SeedCompanyAsync(cancellationToken);
        }

        await SeedDemoUsersAsync(cancellationToken);

        _logger.LogInformation("Datos demo sembrados correctamente.");
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (string roleName in Roles.All)
        {
            if (await _roleManager.FindByNameAsync(roleName) is null)
            {
                IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole(roleName));

                ThrowIfFailed(result, $"No se pudo crear el rol {roleName}.");
            }
        }
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

    private async Task SeedDemoUsersAsync(CancellationToken cancellationToken)
    {
        Company company = await _context.Companies
            .OrderBy(c => c.CreatedAtUtc)
            .FirstAsync(cancellationToken);

        await SeedUserIfNeededAsync(
            "admin@servicedesk.local",
            "Admin",
            "ServiceDesk",
            "Admin*123456",
            Roles.Administrador,
            company,
            cancellationToken);

        await SeedUserIfNeededAsync(
            "tecnico@servicedesk.local",
            "Tecnico",
            "ServiceDesk",
            "Tecnico*123456",
            Roles.Tecnico,
            company,
            cancellationToken);

        await SeedUserIfNeededAsync(
            "cliente@servicedesk.local",
            "Cliente",
            "ServiceDesk",
            "Cliente*123456",
            Roles.Cliente,
            company,
            cancellationToken);
    }

    private async Task SeedUserIfNeededAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        string role,
        Company company,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CompanyId = company.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            IdentityResult createResult = await _userManager.CreateAsync(user, password);

            ThrowIfFailed(createResult, $"No se pudo crear el usuario demo {email}.");
        }
        else if (!await _userManager.CheckPasswordAsync(user, password))
        {
            IdentityResult removeResult = await _userManager.RemovePasswordAsync(user);

            ThrowIfFailed(removeResult, $"No se pudo remover la contraseña del usuario demo {email}.");

            IdentityResult addResult = await _userManager.AddPasswordAsync(user, password);

            ThrowIfFailed(addResult, $"No se pudo restablecer la contraseña del usuario demo {email}.");
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, role);

            ThrowIfFailed(roleResult, $"No se pudo asignar el rol {role} al usuario demo {email}.");
        }
    }

    private static Category CreateCategory(Company company, string name) =>
        new() { CompanyId = company.Id, Name = name, IsActive = true };

    private static Priority CreatePriority(Company company, string name, int sortOrder) =>
        new() { CompanyId = company.Id, Name = name, SortOrder = sortOrder, IsActive = true };

    private static Status CreateStatus(Company company, string name, int sortOrder, bool isClosed = false) =>
        new() { CompanyId = company.Id, Name = name, SortOrder = sortOrder, IsClosed = isClosed, IsActive = true };

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"{message} {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
