using System.Net;
using System.Net.Http.Json;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.IntegrationTests.Endpoints;

[Collection(IntegrationCollection.Name)]
public sealed class TechnicianTicketsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TechnicianTicketsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAssignedToMe_ReturnsOnlyTicketsAssignedToCurrentTechnician()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid assignedTicketId = await CreateSeedCompanyTicketAsync("Asignado", "Visible", assignedToId: technicianId);
        await CreateSeedCompanyTicketAsync("Sin asignar", "No visible");
        Guid otherTicketId = await CreateTicketForOtherCompanyAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.GetAsync("/api/technician/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        TicketDto visible = Assert.Single(tickets);
        Assert.Equal(assignedTicketId, visible.Id);
        Assert.DoesNotContain(tickets, t => t.Id == otherTicketId);
    }

    [Fact]
    public async Task GetAssignedToMe_ReturnsEmptyArray_WhenNoTicketsAssigned()
    {
        await _factory.ResetTicketsAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.GetAsync("/api/technician/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        Assert.Empty(tickets);
    }

    [Fact]
    public async Task GetAssignedToMe_Returns401_WhenNotAuthenticated()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/technician/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAssignedToMe_Returns403_ForClientRole()
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        HttpResponseMessage response = await client.GetAsync("/api/technician/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_Returns200_AndFinalizesTicket()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync(
            "Para resolver",
            "Listo para finalizar",
            assignedToId: technicianId);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { ResolutionNote = "Equipo reparado y verificado." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TicketDto? ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        Assert.Equal("Resuelto", ticket.StatusName);
        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
        Assert.Equal(1, await _factory.CountTicketCommentsAsync(ticketId));
    }

    [Fact]
    public async Task Resolve_Returns200_WithoutResolutionNote()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync(
            "Para resolver sin nota",
            "Listo para finalizar",
            assignedToId: technicianId);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
        Assert.Equal(0, await _factory.CountTicketCommentsAsync(ticketId));
    }

    [Fact]
    public async Task Resolve_Returns404_WhenTicketIsNotAssignedToCurrentTechnician()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync("Ajeno", "No asignado a mí");

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_Returns404_WhenAdministratorTriesToResolveAssignedTicket()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync(
            "Del técnico",
            "Solo el técnico asignado puede finalizarlo",
            assignedToId: technicianId);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Administrador);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_Returns404_ForUnknownTicket()
    {
        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{Guid.NewGuid()}/resolve",
            new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_Returns400_WhenTicketIsAlreadyResolved()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid resueltoId = await _factory.GetStatusIdAsync(companyId, "Resuelto");
        Guid ticketId = await CreateSeedCompanyTicketAsync(
            "Ya resuelto",
            "No se puede finalizar dos veces",
            statusId: resueltoId,
            assignedToId: technicianId);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_Returns403_ForClientRole()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, _) = await GetSeedAsync();
        Guid ticketId = await CreateSeedCompanyTicketAsync(
            "Sin permiso",
            "Un cliente no puede finalizar",
            assignedToId: technicianId);

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { });

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

    private async Task<(Guid CategoryId, Guid PriorityId, Guid StatusId)> GetCatalogAsync(Guid companyId)
    {
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid priorityId = await _factory.GetPriorityIdAsync(companyId, "Media");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        return (categoryId, priorityId, statusId);
    }

    private async Task<Guid> CreateSeedCompanyTicketAsync(
        string title,
        string description,
        Guid? assignedToId = null,
        Guid? statusId = null)
    {
        (Guid companyId, Guid adminId, _, _) = await GetSeedAsync();
        (Guid categoryId, Guid priorityId, Guid defaultStatusId) = await GetCatalogAsync(companyId);
        return await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            priorityId,
            statusId ?? defaultStatusId,
            adminId,
            title,
            description,
            assignedToId: assignedToId);
    }

    private async Task<Guid> CreateTicketForOtherCompanyAsync()
    {
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Competencia S.A.");
        await _factory.CreateCatalogAsync(otherCompanyId);
        (Guid categoryId, Guid priorityId, Guid statusId) = await GetCatalogAsync(otherCompanyId);
        Guid otherTechnicianId = await _factory.CreateUserAsync(
            $"tecnico-{Guid.NewGuid():N}@competencia.local",
            "Técnico",
            "Competencia",
            otherCompanyId,
            Roles.Tecnico);

        return await _factory.CreateTicketAsync(
            otherCompanyId,
            categoryId,
            priorityId,
            statusId,
            otherTechnicianId,
            "Ticket ajeno",
            "No debería ser visible",
            assignedToId: otherTechnicianId);
    }
}
