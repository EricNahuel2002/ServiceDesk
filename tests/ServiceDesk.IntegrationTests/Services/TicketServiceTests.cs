using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.IntegrationTests.Services;

[Collection(IntegrationCollection.Name)]
public sealed class TicketServiceTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyCompanyTickets()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid mineTicketId = await CreateTicketAsync(companyId, "De mi empresa");
        Guid otherTicketId = await CreateTicketForOtherCompanyAsync();

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            IReadOnlyList<TicketDto> tickets = await service.GetAllAsync();

            TicketDto mine = Assert.Single(tickets);
            Assert.Equal(mineTicketId, mine.Id);
            Assert.DoesNotContain(tickets, t => t.Id == otherTicketId);
        });
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTicketsOrderedByCreationDesc()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        await CreateTicketAsync(companyId, "Antiguo", createdAtUtc: DateTime.UtcNow.AddMinutes(-10));
        await CreateTicketAsync(companyId, "Reciente", createdAtUtc: DateTime.UtcNow);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            IReadOnlyList<TicketDto> tickets = await service.GetAllAsync();

            Assert.Equal(2, tickets.Count);
            Assert.Equal("Reciente", tickets[0].Title);
            Assert.Equal("Antiguo", tickets[1].Title);
        });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTicket()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Por id");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto ticket = await service.GetByIdAsync(ticketId);

            Assert.Equal(ticketId, ticket.Id);
            Assert.Equal("Por id", ticket.Title);
        });
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_ForTicketFromOtherCompany()
    {
        Guid otherTicketId = await CreateTicketForOtherCompanyAsync();
        (_, Guid adminId, _, _) = await GetSeedAsync();
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(otherTicketId));
        });
    }

    [Fact]
    public async Task GetTechniciansAsync_ReturnsOnlyActiveCompanyTechnicians()
    {
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            IReadOnlyList<TechnicianDto> technicians = await service.GetTechniciansAsync();

            Assert.Contains(technicians, t => t.Email == CustomWebApplicationFactory.TechnicianEmail);
            Assert.DoesNotContain(technicians, t => t.Email == CustomWebApplicationFactory.AdminEmail);
            Assert.DoesNotContain(technicians, t => t.Email == CustomWebApplicationFactory.ClientEmail);
        });
    }

    [Fact]
    public async Task GetAssignedToMeAsync_ReturnsOnlyTicketsAssignedToCurrentUser()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid assignedTicketId = await CreateTicketAsync(companyId, "Asignado", assignedToId: technicianId);
        await CreateTicketAsync(companyId, "Sin asignar");

        await _factory.RunAsAsync(technicianId, companyId, Roles.Tecnico, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            IReadOnlyList<TicketDto> tickets = await service.GetAssignedToMeAsync();

            TicketDto assigned = Assert.Single(tickets);
            Assert.Equal(assignedTicketId, assigned.Id);
        });
    }

    [Fact]
    public async Task ResolveAsync_FinalizesTicket_WithResolutionComment()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para resolver", assignedToId: technicianId);

        await _factory.RunAsAsync(technicianId, companyId, Roles.Tecnico, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto resolved = await service.ResolveAsync(
                ticketId,
                new ResolveTicketRequest { ResolutionNote = "Resuelto por el técnico." });

            Assert.Equal("Resuelto", resolved.StatusName);
        });

        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
        Assert.Equal(1, await _factory.CountTicketCommentsAsync(ticketId));
    }

    [Fact]
    public async Task ResolveAsync_ThrowsNotFound_WhenTicketIsNotAssignedToCurrentUser()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "No asignado");

        await _factory.RunAsAsync(technicianId, companyId, Roles.Tecnico, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.ResolveAsync(ticketId, new ResolveTicketRequest()));
        });
    }

    [Fact]
    public async Task ResolveAsync_ThrowsValidation_WhenTicketIsAlreadyResolved()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid resueltoId = await _factory.GetStatusIdAsync(companyId, "Resuelto");
        Guid ticketId = await CreateTicketAsync(
            companyId,
            "Ya resuelto",
            statusId: resueltoId,
            assignedToId: technicianId);

        await _factory.RunAsAsync(technicianId, companyId, Roles.Tecnico, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.ResolveAsync(ticketId, new ResolveTicketRequest()));
        });
    }

    [Fact]
    public async Task UpdateAsync_AppliesPriorityChange()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para actualizar");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto updated = await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    Priority = TicketPriority.Alta
                });

            Assert.Equal(TicketPriority.Alta, updated.Priority);
        });
    }

    [Fact]
    public async Task UpdateAsync_AppliesAssignmentChange()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para reasignar");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto updated = await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    AssignedToId = technicianId
                });

            Assert.Equal(technicianId, updated.AssignedToId);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenAssignedUserIsNotTechnician()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Sin técnico");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = adminId,
                        Priority = TicketPriority.Media
                    }));

            Assert.Contains("AssignedToId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenTechnicianIsFromOtherCompany()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Técnico ajeno");
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Otra Empresa S.A.");
        Guid otherTechnicianId = await _factory.CreateUserAsync(
            $"tecnico2-{Guid.NewGuid():N}@otra.local",
            "Técnico",
            "Ajeno",
            otherCompanyId,
            Roles.Tecnico);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = otherTechnicianId,
                        Priority = TicketPriority.Media
                    }));

            Assert.Contains("AssignedToId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenTechnicianIsInactive()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Técnico inactivo");
        Guid inactiveTechnicianId = await _factory.CreateUserAsync(
            $"tecnico.inactivo-{Guid.NewGuid():N}@servicedesk.local",
            "Técnico",
            "Inactivo",
            companyId,
            Roles.Tecnico,
            isActive: false);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = inactiveTechnicianId,
                        Priority = TicketPriority.Media
                    }));

            Assert.Contains("AssignedToId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_ForUnknownTicket()
    {
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateAsync(
                    Guid.NewGuid(),
                    new UpdateTicketRequest
                    {
                        AssignedToId = technicianId,
                        Priority = TicketPriority.Media
                    }));
        });
    }

    [Fact]
    public async Task UpdateAsync_EnqueuesAssignedNotification_WhenTicketIsReassigned()
    {
        await _factory.ResetTicketsAsync();
        _factory.ResetQueue();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para reasignar");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    AssignedToId = technicianId,
                    Priority = TicketPriority.Media
                });
        });

        string message = Assert.Single(_factory.GetQueueMessages());

        TicketAssignedNotification notification =
            JsonSerializer.Deserialize<TicketAssignedNotification>(message)
            ?? throw new InvalidOperationException("El mensaje encolado no es válido.");

        Assert.Equal("TicketAssigned", notification.EventType);
        Assert.Equal(ticketId, notification.TicketId);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotEnqueue_WhenAssignmentDoesNotChange()
    {
        await _factory.ResetTicketsAsync();
        _factory.ResetQueue();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Ya asignado", assignedToId: technicianId);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    AssignedToId = technicianId,
                    Priority = TicketPriority.Media
                });
        });

        Assert.Empty(_factory.GetQueueMessages());
    }

    [Fact]
    public async Task AssignAsync_SetsTechnicianAndStatus()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para asignar");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto assigned = await service.AssignAsync(
                ticketId,
                new AssignTicketRequest { AssignedToId = technicianId });

            Assert.Equal(technicianId, assigned.AssignedToId);
            Assert.Equal("En Espera", assigned.StatusName);
            Assert.NotNull(assigned.AssignedAtUtc);
        });
    }

    [Fact]
    public async Task AssignAsync_EnqueuesNotifications()
    {
        await _factory.ResetTicketsAsync();
        _factory.ResetQueue();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para asignar notif");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.AssignAsync(
                ticketId,
                new AssignTicketRequest { AssignedToId = technicianId });
        });

        Assert.Single(_factory.GetQueueMessages());
        Assert.Single(_factory.GetClientQueueMessages());
    }

    [Fact]
    public async Task AssignAsync_ThrowsNotFound_ForUnknownTicket()
    {
        (_, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.AssignAsync(Guid.NewGuid(), new AssignTicketRequest { AssignedToId = technicianId }));
        });
    }

    [Fact]
    public async Task AssignAsync_ThrowsValidation_WhenTechnicianIsNotValid()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Técnico inválido");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.AssignAsync(ticketId, new AssignTicketRequest { AssignedToId = Guid.NewGuid() }));
        });
    }

    [Fact]
    public async Task CreateAsync_SetsPriorityNull()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto created = await service.CreateAsync(
                new CreateTicketRequest
                {
                    Title = "Ticket sin prioridad",
                    Description = "Descripción",
                    CategoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware")
                });

            Assert.Null(created.Priority);
            Assert.Null(created.AssignedAtUtc);
        });
    }

    private async Task<(Guid CompanyId, Guid AdminId, Guid TechnicianId, Guid ClientId)> GetSeedAsync()
    {
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        Guid adminId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.AdminEmail);
        Guid technicianId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.TechnicianEmail);
        Guid clientId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.ClientEmail);
        return (companyId, adminId, technicianId, clientId);
    }

    private async Task<Guid> CreateTicketAsync(
        Guid companyId,
        string title,
        Guid? assignedToId = null,
        Guid? statusId = null,
        DateTime? createdAtUtc = null)
    {
        (_, Guid adminId, _, _) = await GetSeedAsync();
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid defaultStatusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        return await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            TicketPriority.Media,
            statusId ?? defaultStatusId,
            adminId,
            title,
            $"Descripción de {title}",
            assignedToId: assignedToId,
            createdAtUtc: createdAtUtc);
    }

    private async Task<Guid> CreateTicketForOtherCompanyAsync()
    {
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Competencia S.A.");
        await _factory.CreateCatalogAsync(otherCompanyId);
        Guid categoryId = await _factory.GetCategoryIdAsync(otherCompanyId, "Hardware");
        Guid statusId = await _factory.GetStatusIdAsync(otherCompanyId, "Nuevo");
        Guid otherUserId = await _factory.CreateUserAsync(
            $"usuario-{Guid.NewGuid():N}@competencia.local",
            "Usuario",
            "Competencia",
            otherCompanyId,
            Roles.Cliente);

        return await _factory.CreateTicketAsync(
            otherCompanyId,
            categoryId,
            TicketPriority.Media,
            statusId,
            otherUserId,
            "Ticket ajeno",
            "No debería ser visible");
    }
}
