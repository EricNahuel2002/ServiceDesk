using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.IntegrationTests.Endpoints;

[Collection(IntegrationCollection.Name)]
public sealed class TicketsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithAttachment_UploadsAndAllowsDownload()
    {
        await _factory.ResetTicketsAsync();
        byte[] fileBytes = [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03];

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        (Guid ticketId, Guid attachmentId) = await CreateTicketWithAttachmentAsync(client, fileBytes);

        HttpResponseMessage downloadResponse = await client.GetAsync(
            $"/api/tickets/{ticketId}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("image/png", downloadResponse.Content.Headers.ContentType?.MediaType);

        byte[] downloaded = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, downloaded);
    }

    [Fact]
    public async Task Create_WithAttachment_PersistsAttachmentMetadata()
    {
        await _factory.ResetTicketsAsync();
        byte[] fileBytes = [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03];

        using HttpClient client = await _factory.CreateClientForRoleAsync(Roles.Cliente);

        (Guid ticketId, Guid attachmentId) = await CreateTicketWithAttachmentAsync(client, fileBytes);

        HttpResponseMessage response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TicketDto>? tickets = await response.Content.ReadFromJsonAsync<List<TicketDto>>();
        Assert.NotNull(tickets);
        TicketDto? ticket = Assert.Single(tickets, t => t.Id == ticketId);
        Assert.NotNull(ticket);
        TicketAttachmentDto attachment = Assert.Single(ticket.Attachments);
        Assert.Equal(attachmentId, attachment.Id);
        Assert.Equal("foto.png", attachment.FileName);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(fileBytes.Length, attachment.SizeInBytes);
    }

    [Fact]
    public async Task DownloadAttachment_Returns404_ForUserFromOtherCompany()
    {
        await _factory.ResetTicketsAsync();
        using HttpClient ownerClient = await _factory.CreateClientForRoleAsync(Roles.Cliente);
        (Guid ticketId, Guid attachmentId) = await CreateTicketWithAttachmentAsync(
            ownerClient,
            [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03]);

        Guid otherCompanyId = await _factory.CreateCompanyAsync("Otra Empresa S.A.");
        string email = $"otro-{Guid.NewGuid():N}@otra.local";
        await _factory.CreateUserWithPasswordAsync(
            email,
            "Cliente*123456",
            "Otro",
            "Cliente",
            otherCompanyId,
            Roles.Cliente);

        using HttpClient client = await _factory.CreateClientForAsync(email, "Cliente*123456");

        HttpResponseMessage response = await client.GetAsync(
            $"/api/tickets/{ticketId}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadAttachment_Returns401_WhenNotAuthenticated()
    {
        await _factory.ResetTicketsAsync();
        using HttpClient ownerClient = await _factory.CreateClientForRoleAsync(Roles.Cliente);
        (Guid ticketId, Guid attachmentId) = await CreateTicketWithAttachmentAsync(
            ownerClient,
            [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03]);

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/tickets/{ticketId}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Guid TicketId, Guid AttachmentId)> CreateTicketWithAttachmentAsync(
        HttpClient client,
        byte[] fileBytes)
    {
        Guid companyId = await _factory.GetCompanyIdAsync(CustomWebApplicationFactory.SeedCompanyName);
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");

        using MultipartFormDataContent content = new();
        content.Add(new StringContent("PC sin encender"), "Title");
        content.Add(new StringContent("La PC no enciende desde la mañana."), "Description");
        content.Add(new StringContent(categoryId.ToString()), "CategoryId");
        content.Add(new ByteArrayContent(fileBytes)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
        }, "files", "foto.png");

        HttpResponseMessage createResponse = await client.PostAsync("/api/tickets", content);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        TicketDto? created = await createResponse.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(created);
        TicketAttachmentDto attachment = Assert.Single(created.Attachments);

        return (created.Id, attachment.Id);
    }
}
