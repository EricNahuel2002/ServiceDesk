using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.IntegrationTests.Fakes;

namespace ServiceDesk.IntegrationTests.Services;

[Collection(IntegrationCollection.Name)]
public sealed class TicketFeedbackTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketFeedbackTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitFeedbackAsync_Positive_SavesFeedbackAndKeepsTicketResolved()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Encuesta positiva", assignedToId: technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto result = await service.SubmitFeedbackAsync(
                ticketId,
                new SubmitTicketFeedbackRequest { WasSolved = true, Rating = 5, Comment = "Excelente atención." });

            Assert.Equal("Resuelto", result.StatusName);
            Assert.False(result.HasPendingFeedback);
        });

        using IServiceScope assertScope = _factory.Services.CreateScope();
        ServiceDeskDbContext context = assertScope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        TicketFeedback feedback = await context.TicketFeedbacks.SingleAsync(f => f.TicketId == ticketId);
        Assert.True(feedback.WasSolved);
        Assert.Equal(5, feedback.Rating);
        Assert.Equal("Excelente atención.", feedback.Comment);
        Assert.Equal(technicianId, feedback.TechnicianId);
        Assert.NotNull(await _factory.GetResolvedAtUtcAsync(ticketId));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_Negative_ReopensTicketAndCancelsSlaRecord()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Encuesta negativa", assignedToId: technicianId);
        await AddCurrentSlaRecordAsync(ticketId, technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto result = await service.SubmitFeedbackAsync(
                ticketId,
                new SubmitTicketFeedbackRequest { WasSolved = false });

            Assert.Equal("Nuevo", result.StatusName);
            Assert.Null(result.AssignedToId);
            Assert.Null(result.ResolvedAtUtc);
            Assert.Null(result.StartedWorkAtUtc);
            Assert.True(result.CanReportTechnician);
        });

        using IServiceScope assertScope = _factory.Services.CreateScope();
        ServiceDeskDbContext context = assertScope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        Ticket ticket = await context.Tickets.Include(t => t.SlaRecords).SingleAsync(t => t.Id == ticketId);
        Assert.Null(ticket.ResolvedAtUtc);
        Assert.Null(ticket.StartedWorkAtUtc);
        Assert.Null(ticket.AssignedToId);
        TicketSlaRecord slaRecord = Assert.Single(ticket.SlaRecords);
        Assert.NotNull(slaRecord.CanceledAtUtc);
        Assert.Equal(SlaRecordCancelReason.ReopenedByClientFeedback, slaRecord.CanceledReason);
        TicketComment internalComment =
            await context.TicketComments.SingleAsync(c => c.TicketId == ticketId && c.IsInternal);
        Assert.Contains("no fue resuelto", internalComment.Body);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsValidation_WhenTicketIsNotResolved()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, _, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Sin resolver");

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.SubmitFeedbackAsync(ticketId, new SubmitTicketFeedbackRequest { WasSolved = true }));
        });
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsValidation_WhenAlreadyAnswered()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Encuesta duplicada", assignedToId: technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.SubmitFeedbackAsync(
                ticketId,
                new SubmitTicketFeedbackRequest { WasSolved = true, Rating = 4 });

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.SubmitFeedbackAsync(
                    ticketId,
                    new SubmitTicketFeedbackRequest { WasSolved = true, Rating = 5 }));
        });
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsNotFound_ForTicketFromOtherCompany()
    {
        await _factory.ResetTicketsAsync();
        (_, _, Guid technicianId, _) = await GetSeedAsync();
        Guid otherCompanyId = await _factory.CreateCompanyAsync("Feedback Competencia S.A.");
        await _factory.CreateCatalogAsync(otherCompanyId);
        Guid categoryId = await _factory.GetCategoryIdAsync(otherCompanyId, "Hardware");
        Guid statusId = await _factory.GetStatusIdAsync(otherCompanyId, "Nuevo");
        Guid otherClientId = await _factory.CreateUserAsync(
            $"cliente-{Guid.NewGuid():N}@competencia.local",
            "Cliente",
            "Ajeno",
            otherCompanyId,
            Roles.Cliente);
        Guid otherTicketId = await _factory.CreateTicketAsync(
            otherCompanyId,
            categoryId,
            TicketPriority.Media,
            statusId,
            otherClientId,
            "Ticket ajeno",
            "Descripción",
            assignedToId: technicianId);

        (Guid companyId, _, _, Guid clientId) = await GetSeedAsync();

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SubmitFeedbackAsync(otherTicketId, new SubmitTicketFeedbackRequest { WasSolved = true }));
        });
    }

    [Fact]
    public async Task CreateTechnicianReportAsync_CreatesReportWithAttachments()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Reporte con evidencia", assignedToId: technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);
        await SubmitNegativeFeedbackAsync(companyId, clientId, ticketId);

        byte[] fileBytes = [0x89, 0x50, 0x4E, 0x47];

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            TicketDto result = await service.CreateTechnicianReportAsync(
                ticketId,
                new CreateTechnicianReportRequest
                {
                    Reason = "No resolvió el problema.",
                    Files =
                    [
                        new TicketFileUpload
                        {
                            FileName = "captura.png",
                            ContentType = "image/png",
                            SizeInBytes = fileBytes.Length,
                            Content = new MemoryStream(fileBytes)
                        }
                    ]
                });

            Assert.False(result.CanReportTechnician);
        });

        using IServiceScope assertScope = _factory.Services.CreateScope();
        ServiceDeskDbContext context = assertScope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        Domain.Tickets.TechnicianReport report =
            await context.TechnicianReports.Include(r => r.Attachments).SingleAsync(r => r.TicketId == ticketId);
        Assert.Equal(technicianId, report.TechnicianId);
        Assert.Equal("No resolvió el problema.", report.Reason);
        TechnicianReportAttachment attachment = Assert.Single(report.Attachments);
        Assert.Equal("captura.png", attachment.FileName);

        FakeBlobStorageService blobStorage =
            (FakeBlobStorageService)_factory.Services.GetRequiredService<IBlobStorageService>();
        Stream blob = await blobStorage.DownloadAsync(attachment.BlobName);
        using MemoryStream downloaded = new();
        await blob.CopyToAsync(downloaded);
        Assert.Equal(fileBytes, downloaded.ToArray());
    }

    [Fact]
    public async Task CreateTechnicianReportAsync_ThrowsValidation_WhenThereIsNoNegativeFeedback()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Reporte sin reapertura", assignedToId: technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateTechnicianReportAsync(ticketId, new CreateTechnicianReportRequest()));
        });
    }

    [Fact]
    public async Task CreateTechnicianReportAsync_ThrowsValidation_WhenAlreadyReported()
    {
        await _factory.ResetTicketsAsync();
        (Guid companyId, _, Guid technicianId, Guid clientId) = await GetSeedAsync();
        Guid ticketId = await CreateClientTicketAsync(companyId, "Reporte duplicado", assignedToId: technicianId);
        await ResolveTicketAsync(companyId, technicianId, ticketId);
        await SubmitNegativeFeedbackAsync(companyId, clientId, ticketId);

        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.CreateTechnicianReportAsync(ticketId, new CreateTechnicianReportRequest());

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateTechnicianReportAsync(ticketId, new CreateTechnicianReportRequest()));
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

    private async Task<Guid> CreateClientTicketAsync(Guid companyId, string title, Guid? assignedToId = null)
    {
        Guid categoryId = await _factory.GetCategoryIdAsync(companyId, "Hardware");
        Guid statusId = await _factory.GetStatusIdAsync(companyId, "Nuevo");
        Guid clientId = await _factory.GetUserIdByEmailAsync(CustomWebApplicationFactory.ClientEmail);

        return await _factory.CreateTicketAsync(
            companyId,
            categoryId,
            TicketPriority.Media,
            statusId,
            clientId,
            title,
            $"Descripción de {title}",
            assignedToId: assignedToId);
    }

    private async Task ResolveTicketAsync(Guid companyId, Guid technicianId, Guid ticketId)
    {
        await _factory.RunAsAsync(technicianId, companyId, Roles.Tecnico, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.ResolveAsync(
                ticketId,
                new ResolveTicketRequest { ResolutionNote = "Resuelto por el técnico." });
        });
    }

    private async Task SubmitNegativeFeedbackAsync(
        Guid companyId,
        Guid clientId,
        Guid ticketId)
    {
        await _factory.RunAsAsync(clientId, companyId, Roles.Cliente, async scope =>
        {
            ITicketService service = scope.ServiceProvider.GetRequiredService<ITicketService>();

            await service.SubmitFeedbackAsync(
                ticketId,
                new SubmitTicketFeedbackRequest { WasSolved = false });
        });
    }

    private async Task AddCurrentSlaRecordAsync(Guid ticketId, Guid technicianId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();

        context.TicketSlaRecords.Add(new TicketSlaRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            TechnicianId = technicianId,
            Priority = TicketPriority.Media,
            SlaLimitHours = 4,
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(4),
            IsCurrent = true
        });

        await context.SaveChangesAsync();
    }
}
