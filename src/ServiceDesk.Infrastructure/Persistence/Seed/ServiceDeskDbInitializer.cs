using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

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

        await SeedDemoTicketsAsync(cancellationToken);

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

    private async Task SeedDemoTicketsAsync(CancellationToken cancellationToken)
    {
        Company company = await _context.Companies
            .OrderBy(c => c.CreatedAtUtc)
            .FirstAsync(cancellationToken);

        bool hasTickets = await _context.Tickets
            .AnyAsync(ticket => ticket.CompanyId == company.Id, cancellationToken);

        if (hasTickets)
        {
            return;
        }

        ApplicationUser cliente = await GetUserByEmailAsync("cliente@servicedesk.local", cancellationToken);
        ApplicationUser tecnico = await GetUserByEmailAsync("tecnico@servicedesk.local", cancellationToken);

        Guid hardwareId = await FindCatalogIdAsync(
            _context.Categories,
            category => category.CompanyId == company.Id && category.Name == "Hardware",
            cancellationToken);

        Guid softwareId = await FindCatalogIdAsync(
            _context.Categories,
            category => category.CompanyId == company.Id && category.Name == "Software",
            cancellationToken);

        Guid redId = await FindCatalogIdAsync(
            _context.Categories,
            category => category.CompanyId == company.Id && category.Name == "Red",
            cancellationToken);

        Guid bajaId = await FindCatalogIdAsync(
            _context.Priorities,
            priority => priority.CompanyId == company.Id && priority.Name == "Baja",
            cancellationToken);

        Guid mediaId = await FindCatalogIdAsync(
            _context.Priorities,
            priority => priority.CompanyId == company.Id && priority.Name == "Media",
            cancellationToken);

        Guid altaId = await FindCatalogIdAsync(
            _context.Priorities,
            priority => priority.CompanyId == company.Id && priority.Name == "Alta",
            cancellationToken);

        Guid nuevoId = await FindCatalogIdAsync(
            _context.Statuses,
            status => status.CompanyId == company.Id && status.Name == "Nuevo",
            cancellationToken);

        Guid enProgresoId = await FindCatalogIdAsync(
            _context.Statuses,
            status => status.CompanyId == company.Id && status.Name == "En Progreso",
            cancellationToken);

        Guid resueltoId = await FindCatalogIdAsync(
            _context.Statuses,
            status => status.CompanyId == company.Id && status.Name == "Resuelto",
            cancellationToken);

        _context.Tickets.AddRange(
            CreateTicket(
                company.Id,
                cliente.Id,
                hardwareId,
                mediaId,
                nuevoId,
                "PC no enciende",
                "La PC de recepción no enciende desde esta mañana."),
            CreateTicket(
                company.Id,
                cliente.Id,
                redId,
                altaId,
                enProgresoId,
                "VPN no funciona",
                "No puedo conectarme a la VPN desde casa.",
                assignedToId: tecnico.Id),
            CreateTicket(
                company.Id,
                cliente.Id,
                hardwareId,
                mediaId,
                resueltoId,
                "Impresora no imprime",
                "La impresora del piso 3 quedó con un atasco de papel.",
                assignedToId: tecnico.Id,
                resolved: true),
            CreateTicket(
                company.Id,
                cliente.Id,
                softwareId,
                bajaId,
                nuevoId,
                "Solicitud de licencia",
                "Necesito una licencia de Microsoft 365 para el equipo de ventas."));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tickets demo sembrados.");
    }

    private async Task<ApplicationUser> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            throw new InvalidOperationException($"No se encontró el usuario demo {email}.");
        }

        return user;
    }

    private static async Task<Guid> FindCatalogIdAsync<TEntity>(
        IQueryable<TEntity> entities,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        Guid? id = await entities
            .Where(predicate)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (id is null)
        {
            throw new InvalidOperationException("No se encontró un catálogo requerido para la empresa demo.");
        }

        return id.Value;
    }

    private static Ticket CreateTicket(
        Guid companyId,
        Guid createdById,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        string title,
        string description,
        Guid? assignedToId = null,
        bool resolved = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedById = createdById,
            CategoryId = categoryId,
            PriorityId = priorityId,
            StatusId = statusId,
            Title = title,
            Description = description,
            AssignedToId = assignedToId,
            ResolvedAtUtc = resolved ? DateTime.UtcNow : null
        };

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
