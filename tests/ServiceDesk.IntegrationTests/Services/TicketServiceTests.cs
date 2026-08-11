using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
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
    public async Task UpdateAsync_AppliesChanges()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para actualizar");
        Guid altaId = await _factory.GetPriorityIdAsync(companyId, "Alta");
        Guid enProgresoId = await _factory.GetStatusIdAsync(companyId, "En Progreso");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto updated = await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    AssignedToId = technicianId,
                    PriorityId = altaId,
                    StatusId = enProgresoId
                });

            Assert.Equal(technicianId, updated.AssignedToId);
            Assert.Equal(altaId, updated.PriorityId);
            Assert.Equal(enProgresoId, updated.StatusId);
        });

        Assert.Null(await _factory.GetResolvedAtUtcAsync(ticketId));
    }

    [Fact]
    public async Task UpdateAsync_SetsResolvedAtUtc_WhenStatusIsClosed()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Para cerrar");
        Guid resueltoId = await _factory.GetStatusIdAsync(companyId, "Resuelto");

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.UpdateAsync(
                ticketId,
                new UpdateTicketRequest
                {
                    AssignedToId = technicianId,
                    PriorityId = (await GetCatalogAsync(companyId)).PriorityId,
                    StatusId = resueltoId
                });
        });

        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenAssignedUserIsNotTechnician()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Sin técnico");
        (_, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = adminId,
                        PriorityId = priorityId,
                        StatusId = statusId
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
        (_, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);
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
                        PriorityId = priorityId,
                        StatusId = statusId
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
        (_, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);
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
                        PriorityId = priorityId,
                        StatusId = statusId
                    }));

            Assert.Contains("AssignedToId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenPriorityDoesNotExist()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Prioridad inválida");
        (_, _, Guid statusId) = await GetCatalogAsync(companyId);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = technicianId,
                        PriorityId = Guid.NewGuid(),
                        StatusId = statusId
                    }));

            Assert.Contains("PriorityId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenStatusDoesNotExist()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateTicketAsync(companyId, "Estado inválido");
        (_, Guid priorityId, _) = await GetCatalogAsync(companyId);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(
                    ticketId,
                    new UpdateTicketRequest
                    {
                        AssignedToId = technicianId,
                        PriorityId = priorityId,
                        StatusId = Guid.NewGuid()
                    }));

            Assert.Contains("StatusId", exception.Errors.Keys);
        });
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_ForUnknownTicket()
    {
        (Guid companyId, Guid adminId, Guid technicianId, _) = await GetSeedAsync();
        (_, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);

        await _factory.RunAsAsync(adminId, companyId, Roles.Administrador, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateAsync(
                    Guid.NewGuid(),
                    new UpdateTicketRequest
                    {
                        AssignedToId = technicianId,
                        PriorityId = priorityId,
                        StatusId = statusId
                    }));
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

    private async Task<(Guid CategoryId, Guid PriorityId, Guid StatusId)> GetCatalogAsync(Guid companyId)
    {
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid priorityId = await _factory.GetPriorityIdAsync(companyId, "Media");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        return (categoryId, priorityId, statusId);
    }

    private async Task<Guid> CreateTicketAsync(Guid companyId, string title, DateTime? createdAtUtc = null)
    {
        (_, Guid adminId, _, _) = await GetSeedAsync();
        (Guid categoryId, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);
        return await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            priorityId,
            statusId,
            adminId,
            title,
            $"Descripción de {title}",
            createdAtUtc: createdAtUtc);
    }

    private async Task<Guid> CreateTicketForOtherCompanyAsync()
    {
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Competencia S.A.");
        await _factory.CreateCatalogAsync(otherCompanyId);
        (Guid categoryId, Guid priorityId, Guid statusId) = await GetCatalogAsync(otherCompanyId);
        Guid otherUserId = await _factory.CreateUserAsync(
            $"usuario-{Guid.NewGuid():N}@competencia.local",
            "Usuario",
            "Competencia",
            otherCompanyId,
            Roles.Cliente);

        return await _factory.CreateTicketAsync(
            otherCompanyId,
            categoryId,
            priorityId,
            statusId,
            otherUserId,
            "Ticket ajeno",
            "No debería ser visible");
    }
}
