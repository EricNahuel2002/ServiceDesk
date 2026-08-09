using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.IntegrationTests.Endpoints;

[Collection(IntegrationCollection.Name)]
public sealed class AdminTicketsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminTicketsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_Returns200_WithCompanyTickets()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket A", "Descripción A");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        TicketDto ticket = Assert.Single(tickets);
        Assert.Equal(ticketId, ticket.Id);
        Assert.Equal("Ticket A", ticket.Title);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyArray_WhenThereAreNoTickets()
    {
        await _factory.ResetTicketsAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        Assert.Empty(tickets);
    }

    [Fact]
    public async Task GetAll_ExcludesTicketsFromOtherCompanies()
    {
        await _factory.ResetTicketsAsync();
        Guid companyTicketId = await CreateSeedCompanyTicketAsync("De mi empresa", "Visible");
        Guid otherTicketId = await CreateTicketForOtherCompanyAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        TicketDto visible = Assert.Single(tickets);
        Assert.Equal(companyTicketId, visible.Id);
        Assert.DoesNotContain(tickets, t => t.Id == otherTicketId);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenNotAuthenticated()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Cliente)]
    [InlineData(Roles.Tecnico)]
    public async Task GetAll_Returns403_ForNonAdminRoles(string role)
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(role);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns200_ForExistingTicket()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket B", "Detalle");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync($"/api/admin/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TicketDto? ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        Assert.Equal(ticketId, ticket.Id);
        Assert.Equal("Ticket B", ticket.Title);
    }

    [Fact]
    public async Task GetById_Returns404_ForUnknownTicket()
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync($"/api/admin/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_ForTicketFromOtherCompany()
    {
        Guid otherTicketId = await CreateTicketForOtherCompanyAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync($"/api/admin/tickets/{otherTicketId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns403_ForClientRole()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket C", "Privado");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        HttpResponseMessage response = await client.GetAsync($"/api/admin/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTechnicians_ReturnsOnlyCompanyTechnicians()
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets/technicians");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TechnicianDto>? technicians = await response.Content.ReadFromJsonAsync<List<TechnicianDto>>();
        Assert.NotNull(technicians);
        Assert.Contains(technicians, t => t.Email == CustomWebApplicationFactory.TechnicianEmail);
        Assert.DoesNotContain(technicians, t => t.Email == CustomWebApplicationFactory.AdminEmail);
        Assert.DoesNotContain(technicians, t => t.Email == CustomWebApplicationFactory.ClientEmail);
    }

    [Fact]
    public async Task GetTechnicians_ReturnsEmpty_WhenCompanyHasNoTechnicians()
    {
        Guid companyId = await _factory.CreateCompanyAsync("Sin Técnicos S.A.");
        string adminEmail = $"admin2-{Guid.NewGuid():N}@servicedesk.test";
        await _factory.CreateUserWithPasswordAsync(
            adminEmail,
            "Admin2*123456",
            "Admin",
            "Dos",
            companyId,
            Roles.Administrador);

        using HttpClient client = await _factory.CreateClientForAsync(adminEmail, "Admin2*123456");

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets/technicians");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TechnicianDto>? technicians = await response.Content.ReadFromJsonAsync<List<TechnicianDto>>();
        Assert.NotNull(technicians);
        Assert.Empty(technicians);
    }

    [Theory]
    [InlineData(Roles.Cliente)]
    [InlineData(Roles.Tecnico)]
    public async Task GetTechnicians_Returns403_ForNonAdminRoles(string role)
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(role);

        HttpResponseMessage response = await client.GetAsync("/api/admin/tickets/technicians");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns200_AndAppliesAssignedToPriorityAndStatus()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket D", "Para actualizar");
        Guid altaId = await _factory.GetPriorityIdAsync(companyId, "Alta");
        Guid enProgresoId = await _factory.GetStatusIdAsync(companyId, "En Progreso");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new { AssignedToId = technicianId, PriorityId = altaId, StatusId = enProgresoId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TicketDto? ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        Assert.Equal(technicianId, ticket.AssignedToId);
        Assert.Equal(altaId, ticket.PriorityId);
        Assert.Equal(enProgresoId, ticket.StatusId);
        Assert.Equal("Alta", ticket.PriorityName);
        Assert.Equal("En Progreso", ticket.StatusName);
    }

    [Fact]
    public async Task Update_SetsResolvedAtUtc_WhenStatusIsClosed()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket E", "Para resolver");
        Guid resueltoId = await _factory.GetStatusIdAsync(companyId, "Resuelto");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = technicianId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = resueltoId
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
    }

    [Fact]
    public async Task Update_ClearsResolvedAtUtc_WhenStatusIsReopened()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket F", "Para reabrir");
        Guid resueltoId = await _factory.GetStatusIdAsync(companyId, "Resuelto");
        Guid nuevoId = await _factory.GetStatusIdAsync(companyId, "Nuevo");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new { AssignedToId = technicianId, PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId, StatusId = resueltoId });

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new { AssignedToId = technicianId, PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId, StatusId = nuevoId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await _factory.GetResolvedAtUtcAsync(ticketId));
    }

    [Fact]
    public async Task Update_Returns404_ForUnknownTicket()
    {
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{Guid.NewGuid()}",
            new
            {
                AssignedToId = technicianId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = (await GetSeedCompanyCatalogAsync()).StatusId
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns400_WhenAssignedUserIsNotTechnician()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket G", "Técnico inválido");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = adminId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = (await GetSeedCompanyCatalogAsync()).StatusId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("AssignedToId", await GetValidationErrorsAsync(response));
    }

    [Fact]
    public async Task Update_Returns400_WhenTechnicianBelongsToAnotherCompany()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket H", "Técnico ajeno");
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Otra Empresa S.A.");
        Guid otherTechnicianId = await _factory.CreateUserAsync(
            $"tecnico2-{Guid.NewGuid():N}@otra.local",
            "Técnico",
            "Ajeno",
            otherCompanyId,
            Roles.Tecnico);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = otherTechnicianId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = (await GetSeedCompanyCatalogAsync()).StatusId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("AssignedToId", await GetValidationErrorsAsync(response));
    }

    [Fact]
    public async Task Update_Returns400_WhenTechnicianIsInactive()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, _, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket I", "Técnico inactivo");
        Guid inactiveTechnicianId = await _factory.CreateUserAsync(
            $"tecnico.inactivo-{Guid.NewGuid():N}@servicedesk.local",
            "Técnico",
            "Inactivo",
            companyId,
            Roles.Tecnico,
            isActive: false);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = inactiveTechnicianId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = (await GetSeedCompanyCatalogAsync()).StatusId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("AssignedToId", await GetValidationErrorsAsync(response));
    }

    [Fact]
    public async Task Update_Returns400_WhenPriorityDoesNotExist()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket J", "Prioridad inválida");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = technicianId,
                PriorityId = Guid.NewGuid(),
                StatusId = (await GetSeedCompanyCatalogAsync()).StatusId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("PriorityId", await GetValidationErrorsAsync(response));
    }

    [Fact]
    public async Task Update_Returns400_WhenStatusDoesNotExist()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket K", "Estado inválido");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new
            {
                AssignedToId = technicianId,
                PriorityId = (await GetSeedCompanyCatalogAsync()).PriorityId,
                StatusId = Guid.NewGuid()
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("StatusId", await GetValidationErrorsAsync(response));
    }

    [Fact]
    public async Task Update_Returns400_WhenRequiredFieldsAreEmpty()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket L", "Campos vacíos");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new { AssignedToId = Guid.Empty, PriorityId = Guid.Empty, StatusId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        IReadOnlyCollection<string> errors = await GetValidationErrorsAsync(response);
        Assert.Contains("AssignedToId", errors);
        Assert.Contains("PriorityId", errors);
        Assert.Contains("StatusId", errors);
    }

    [Fact]
    public async Task Update_Returns403_ForNonAdminRoles()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ticket M", "Sin permiso");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/admin/tickets/{ticketId}",
            new { AssignedToId = Guid.NewGuid(), PriorityId = Guid.NewGuid(), StatusId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(Guid CompanyId, Guid AdminId, Guid TechnicianId, Guid ClientId)> GetSeedAsync()
    {
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        Guid adminId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.AdminEmail);
        Guid technicianId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.TechnicianEmail);
        Guid clientId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.ClientEmail);
        return (companyId, adminId, technicianId, clientId);
    }

    private async Task<(Guid CategoryId, Guid PriorityId, Guid StatusId)> GetSeedCompanyCatalogAsync()
    {
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        return await GetCatalogAsync(companyId);
    }

    private async Task<(Guid CategoryId, Guid PriorityId, Guid StatusId)> GetCatalogAsync(Guid companyId)
    {
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid priorityId = await _factory.GetPriorityIdAsync(companyId, "Media");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        return (categoryId, priorityId, statusId);
    }

    private async Task<Guid> CreateSeedCompanyTicketAsync(string title, string description)
    {
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        (Guid categoryId, Guid priorityId, Guid statusId) = await GetCatalogAsync(companyId);
        return await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            priorityId,
            statusId,
            adminId,
            title,
            description);
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

    private static async Task<IReadOnlyCollection<string>> GetValidationErrorsAsync(HttpResponseMessage response)
    {
        ValidationProblemDetails? details = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        return details?.Errors.Keys.ToArray() ?? [];
    }
}
