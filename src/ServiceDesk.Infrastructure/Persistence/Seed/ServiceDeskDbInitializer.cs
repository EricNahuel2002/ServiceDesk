using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Sla;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Seed;

public sealed class ServiceDeskDbInitializer
{
    private readonly ServiceDeskDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<ServiceDeskDbInitializer> _logger;

    private static readonly JsonSerializerOptions BusinessHoursJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

        await EnsureSlaConfigurationsAsync(cancellationToken);

        await EnsureBusinessHoursAsync(cancellationToken);

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

        _context.Statuses.AddRange(
            CreateStatus(company, "Nuevo", 1),
            CreateStatus(company, "En Progreso", 2),
            CreateStatus(company, "En Espera", 3),
            CreateStatus(company, "Resuelto", 4, isClosed: true),
            CreateStatus(company, "Cerrado", 5, isClosed: true));

        _context.SlaConfigurations.AddRange(
            new SlaConfiguration { CompanyId = company.Id, Priority = TicketPriority.Baja, ResponseTimeHours = 8 },
            new SlaConfiguration { CompanyId = company.Id, Priority = TicketPriority.Media, ResponseTimeHours = 4 },
            new SlaConfiguration { CompanyId = company.Id, Priority = TicketPriority.Alta, ResponseTimeHours = 2 },
            new SlaConfiguration { CompanyId = company.Id, Priority = TicketPriority.Critica, ResponseTimeHours = 1 });

        string businessHoursJson = JsonSerializer.Serialize(new Dictionary<string, DaySchedule>
        {
            ["Monday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Tuesday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Wednesday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Thursday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Friday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Saturday"] = new() { Enabled = false },
            ["Sunday"] = new() { Enabled = false }
        }, BusinessHoursJsonOptions);

        _context.CompanyBusinessHours.Add(new CompanyBusinessHours
        {
            CompanyId = company.Id,
            TimeZoneId = "Argentina Standard Time",
            BusinessHoursJson = businessHoursJson,
            UseBusinessHours = true,
            MaxAssignmentToStartMinutes = 120
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSlaConfigurationsAsync(CancellationToken cancellationToken)
    {
        Guid companyId = await _context.Companies
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => c.Id)
            .FirstAsync(cancellationToken);

        bool hasSlaConfigs = await _context.SlaConfigurations
            .AnyAsync(s => s.CompanyId == companyId, cancellationToken);

        if (hasSlaConfigs)
        {
            return;
        }

        _context.SlaConfigurations.AddRange(
            new SlaConfiguration { CompanyId = companyId, Priority = TicketPriority.Baja, ResponseTimeHours = 8 },
            new SlaConfiguration { CompanyId = companyId, Priority = TicketPriority.Media, ResponseTimeHours = 4 },
            new SlaConfiguration { CompanyId = companyId, Priority = TicketPriority.Alta, ResponseTimeHours = 2 },
            new SlaConfiguration { CompanyId = companyId, Priority = TicketPriority.Critica, ResponseTimeHours = 1 });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBusinessHoursAsync(CancellationToken cancellationToken)
    {
        Guid companyId = await _context.Companies
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => c.Id)
            .FirstAsync(cancellationToken);

        bool hasBusinessHours = await _context.CompanyBusinessHours
            .AnyAsync(b => b.CompanyId == companyId, cancellationToken);

        if (hasBusinessHours)
        {
            return;
        }

        string businessHoursJson = JsonSerializer.Serialize(new Dictionary<string, DaySchedule>
        {
            ["Monday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Tuesday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Wednesday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Thursday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Friday"] = new() { Enabled = true, Start = "08:00", End = "17:00" },
            ["Saturday"] = new() { Enabled = false },
            ["Sunday"] = new() { Enabled = false }
        }, BusinessHoursJsonOptions);

        _context.CompanyBusinessHours.Add(new CompanyBusinessHours
        {
            CompanyId = companyId,
            TimeZoneId = "Argentina Standard Time",
            BusinessHoursJson = businessHoursJson,
            UseBusinessHours = true,
            MaxAssignmentToStartMinutes = 120
        });

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

        DateTime now = DateTime.UtcNow;

        Ticket ticket1 = CreateTicket(
            company.Id,
            cliente.Id,
            hardwareId,
            null,
            nuevoId,
            "PC no enciende",
            "La PC de recepción no enciende desde esta mañana.",
            responseDeadline: now.AddHours(4));

        Ticket ticket2 = CreateTicket(
            company.Id,
            cliente.Id,
            redId,
            TicketPriority.Alta,
            enProgresoId,
            "VPN no funciona",
            "No puedo conectarme a la VPN desde casa.",
            assignedToId: tecnico.Id,
            assignedAtUtc: now.AddHours(-1),
            responseDeadline: now.AddHours(2),
            startedWorkAt: now.AddMinutes(-30));

        Ticket ticket3 = CreateTicket(
            company.Id,
            cliente.Id,
            hardwareId,
            TicketPriority.Media,
            resueltoId,
            "Impresora no imprime",
            "La impresora del piso 3 quedó con un atasco de papel.",
            assignedToId: tecnico.Id,
            assignedAtUtc: now.AddHours(-2),
            resolved: true,
            responseDeadline: now.AddHours(4),
            startedWorkAt: now.AddMinutes(-60));

        Ticket ticket4 = CreateTicket(
            company.Id,
            cliente.Id,
            softwareId,
            null,
            nuevoId,
            "Solicitud de licencia",
            "Necesito una licencia de Microsoft 365 para el equipo de ventas.",
            responseDeadline: now.AddHours(8));

        _context.Tickets.AddRange(ticket1, ticket2, ticket3, ticket4);

        _context.AuditLogs.AddRange(
            CreateAuditLog(company.Id, cliente.Id, ticket1.Id, TicketAuditActions.Created, "Ticket creado",
                now.AddMinutes(-5)),
            CreateAuditLog(company.Id, cliente.Id, ticket2.Id, TicketAuditActions.Created, "Ticket creado",
                now.AddHours(-2)),
            CreateAuditLog(company.Id, cliente.Id, ticket2.Id, TicketAuditActions.Assigned, $"Asignado a {tecnico.FirstName} {tecnico.LastName}",
                now.AddHours(-1)),
            CreateAuditLog(company.Id, tecnico.Id, ticket2.Id, TicketAuditActions.WorkStarted, "Trabajo iniciado",
                now.AddMinutes(-30)),
            CreateAuditLog(company.Id, cliente.Id, ticket3.Id, TicketAuditActions.Created, "Ticket creado",
                now.AddHours(-4)),
            CreateAuditLog(company.Id, cliente.Id, ticket3.Id, TicketAuditActions.Assigned, $"Asignado a {tecnico.FirstName} {tecnico.LastName}",
                now.AddHours(-3)),
            CreateAuditLog(company.Id, tecnico.Id, ticket3.Id, TicketAuditActions.WorkStarted, "Trabajo iniciado",
                now.AddHours(-1)),
            CreateAuditLog(company.Id, tecnico.Id, ticket3.Id, TicketAuditActions.Resolved, "Ticket cerrado",
                now.AddMinutes(-30)),
            CreateAuditLog(company.Id, cliente.Id, ticket4.Id, TicketAuditActions.Created, "Ticket creado",
                now.AddMinutes(-10)));

        _context.ChatMessages.AddRange(
            CreateChatMessage(ticket2.Id, cliente.Id, "Hola, no puedo conectarme a la VPN desde mi casa.", now.AddHours(-1)),
            CreateChatMessage(ticket2.Id, tecnico.Id, "Hola, ¿qué versión de cliente VPN estás usando?", now.AddMinutes(-50)),
            CreateChatMessage(ticket2.Id, cliente.Id, "Creo que es la versión 5.0.", now.AddMinutes(-45)),
            CreateChatMessage(ticket2.Id, tecnico.Id, "Gracias, voy a revisar la configuración del servidor.", now.AddMinutes(-35)),
            CreateChatMessage(ticket3.Id, cliente.Id, "La impresora del piso 3 tiene un atasco de papel.", now.AddHours(-4)),
            CreateChatMessage(ticket3.Id, tecnico.Id, "Voy a revisarla ahora.", now.AddHours(-3)),
            CreateChatMessage(ticket3.Id, tecnico.Id, "Ya quedó lista, podés probar.", now.AddMinutes(-40)));

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
        TicketPriority? priority,
        Guid statusId,
        string title,
        string description,
        DateTime responseDeadline,
        Guid? assignedToId = null,
        DateTime? assignedAtUtc = null,
        bool resolved = false,
        DateTime? startedWorkAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedById = createdById,
            CategoryId = categoryId,
            Priority = priority,
            StatusId = statusId,
            Title = title,
            Description = description,
            AssignedToId = assignedToId,
            AssignedAtUtc = assignedAtUtc,
            ResponseDeadlineAtUtc = responseDeadline,
            StartedWorkAtUtc = startedWorkAt,
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

    private static AuditLog CreateAuditLog(
        Guid companyId,
        Guid userId,
        Guid ticketId,
        string action,
        string description,
        DateTime occurredAtUtc,
        string? details = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = companyId,
            EntityType = "Ticket",
            EntityId = ticketId,
            Action = action,
            Description = description,
            Details = details,
            CreatedAtUtc = occurredAtUtc
        };
    }

    private static ChatMessage CreateChatMessage(Guid ticketId, Guid senderId, string content, DateTime sentAtUtc)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SenderId = senderId,
            Content = content,
            SentAtUtc = sentAtUtc
        };
    }
}
