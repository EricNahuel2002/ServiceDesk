using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.IntegrationTests.Endpoints;

[Collection(IntegrationCollection.Name)]
public sealed class TicketFeedbackEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketFeedbackEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitFeedback_Returns200_AndUpdatesTicket()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateResolvedTicketAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/feedback",
            new { WasSolved = true, Rating = 5, Comment = "Todo funcionó." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TicketDto? ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        Assert.Equal("Resuelto", ticket.StatusName);
        Assert.False(ticket.HasPendingFeedback);
    }

    [Fact]
    public async Task SubmitFeedback_Returns403_ForTechnicianRole()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateResolvedTicketAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Tecnico);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/feedback",
            new { WasSolved = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubmitFeedback_Returns401_WhenNotAuthenticated()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateResolvedTicketAsync();

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/feedback",
            new { WasSolved = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTechnicianReport_Returns201_WithAttachment()
    {
        await _factory.ResetTicketsAsync();
        Guid ticketId = await CreateResolvedTicketAsync();

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);
        await SubmitNegativeFeedbackAsync(client, ticketId);

        using MultipartFormDataContent content = new();
        content.Add(new StringContent("No resolvió nada."), "Reason");
        content.Add(new ByteArrayContent([0x89, 0x50, 0x4E, 0x47])
        {
            Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
        }, "files", "captura.png");

        HttpResponseMessage response = await client.PostAsync(
            $"/api/tickets/{ticketId}/technician-report",
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        TicketDto? ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        Assert.False(ticket.CanReportTechnician);
    }

    private async Task<Guid> CreateResolvedTicketAsync()
    {
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        Guid clientId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.ClientEmail);
        Guid technicianId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.TechnicianEmail);

        Guid ticketId = await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            Domain.Enums.TicketPriority.Media,
            statusId,
            clientId,
            "Ticket para encuesta",
            "Descripción del ticket",
            assignedToId: technicianId);

        using HttpClient technicianClient = await _factory.CreateClientForRoleAsync(Roles.Tecnico);
        HttpResponseMessage resolveResponse = await technicianClient.PatchAsJsonAsync(
            $"/api/technician/tickets/{ticketId}/resolve",
            new { ResolutionNote = "Resuelto." });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        return ticketId;
    }

    private static async Task SubmitNegativeFeedbackAsync(HttpClient client, Guid ticketId)
    {
        HttpResponseMessage feedbackResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/feedback",
            new { WasSolved = false });

        Assert.Equal(HttpStatusCode.OK, feedbackResponse.StatusCode);
    }
}
