using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.DTOs.Audits;
using ServiceDesk.Application.Features.Audits;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Services;

public sealed class AuditServiceTests : IDisposable
{
    private readonly ServiceDeskDbContext _context;
    private readonly AuditService _service;
    private readonly Guid _companyId;
    private readonly Guid _adminId;
    private readonly Guid _tecnicoId;
    private readonly Guid _clienteId;

    public AuditServiceTests()
    {
        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ServiceDeskDbContext(options);

        _companyId = Guid.NewGuid();
        _adminId = Guid.NewGuid();
        _tecnicoId = Guid.NewGuid();
        _clienteId = Guid.NewGuid();

        _context.Companies.Add(new Company { Id = _companyId, Name = "Test Company", IsActive = true });

        _context.Users.AddRange(
            new ApplicationUser
            {
                Id = _adminId,
                UserName = "admin@test.com",
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "Test",
                CompanyId = _companyId,
                EmailConfirmed = true,
                IsActive = true
            },
            new ApplicationUser
            {
                Id = _tecnicoId,
                UserName = "tecnico@test.com",
                Email = "tecnico@test.com",
                FirstName = "Tecnico",
                LastName = "Test",
                CompanyId = _companyId,
                EmailConfirmed = true,
                IsActive = true
            },
            new ApplicationUser
            {
                Id = _clienteId,
                UserName = "cliente@test.com",
                Email = "cliente@test.com",
                FirstName = "Cliente",
                LastName = "Test",
                CompanyId = _companyId,
                EmailConfirmed = true,
                IsActive = true
            });

        _context.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Hardware", IsActive = true },
            new Category { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Software", IsActive = true });

        _context.Statuses.AddRange(
            new Status { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Nuevo", SortOrder = 1, IsActive = true },
            new Status { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "En Progreso", SortOrder = 2, IsActive = true },
            new Status { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Resuelto", SortOrder = 3, IsActive = true, IsClosed = true });

        _context.SaveChanges();

        _service = CreateService();
    }

    [Fact]
    public async Task GetTechniciansAsync_ReturnsTechniciansForCompany()
    {
        var result = await _service.GetTechniciansAsync(CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTicketHistoryAsync_TicketExists_ReturnsEventsInOrder()
    {
        Guid ticketId = Guid.NewGuid();

        _context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Description = "Test",
            CompanyId = _companyId,
            CategoryId = _context.Categories.First(c => c.CompanyId == _companyId).Id,
            StatusId = _context.Statuses.First(s => s.CompanyId == _companyId && s.Name == "Nuevo").Id,
            CreatedById = _clienteId,
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(4)
        });

        _context.SaveChanges();

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _clienteId,
            CompanyId = _companyId,
            EntityType = "Ticket",
            EntityId = ticketId,
            Action = TicketAuditActions.Created,
            Description = "Ticket creado"
        });
        await _context.SaveChangesAsync();

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _tecnicoId,
            CompanyId = _companyId,
            EntityType = "Ticket",
            EntityId = ticketId,
            Action = TicketAuditActions.WorkStarted,
            Description = "Trabajo iniciado"
        });
        await _context.SaveChangesAsync();

        IReadOnlyList<TicketAuditEventDto> history =
            await _service.GetTicketHistoryAsync(ticketId, CancellationToken.None);

        Assert.Equal(2, history.Count);
        Assert.Equal(TicketAuditActions.Created, history[0].Action);
        Assert.Equal(TicketAuditActions.WorkStarted, history[1].Action);
    }

    [Fact]
    public async Task GetTicketHistoryAsync_TicketFromAnotherCompany_ThrowsNotFound()
    {
        Guid ticketId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        _context.Companies.Add(new Company { Id = otherCompanyId, Name = "Other", IsActive = true });
        _context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            Title = "Other Ticket",
            Description = "Test",
            CompanyId = otherCompanyId,
            CategoryId = _context.Categories.First().Id,
            StatusId = _context.Statuses.First().Id,
            CreatedById = _clienteId,
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(4)
        });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetTicketHistoryAsync(ticketId, CancellationToken.None));
    }

    [Fact]
    public async Task GetTicketChatAsync_TicketExists_ReturnsMessagesOrderedAscending()
    {
        Guid ticketId = Guid.NewGuid();

        _context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            Title = "Test Ticket",
            Description = "Test",
            CompanyId = _companyId,
            CategoryId = _context.Categories.First(c => c.CompanyId == _companyId).Id,
            StatusId = _context.Statuses.First(s => s.CompanyId == _companyId).Id,
            CreatedById = _clienteId,
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(4)
        });

        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SenderId = _clienteId,
            Content = "Hello",
            SentAtUtc = DateTime.UtcNow.AddMinutes(-50)
        });
        await _context.SaveChangesAsync();

        _context.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SenderId = _tecnicoId,
            Content = "Hi there",
            SentAtUtc = DateTime.UtcNow.AddMinutes(-45)
        });
        await _context.SaveChangesAsync();

        var chat = await _service.GetTicketChatAsync(ticketId, CancellationToken.None);

        Assert.Equal(2, chat.Count);
        Assert.Equal("Hello", chat[0].Content);
        Assert.Equal("Hi there", chat[1].Content);
    }

    [Fact]
    public async Task GetTicketChatAsync_TicketFromAnotherCompany_ThrowsNotFound()
    {
        Guid ticketId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        _context.Companies.Add(new Company { Id = otherCompanyId, Name = "Other", IsActive = true });
        _context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            Title = "Other Ticket",
            Description = "Test",
            CompanyId = otherCompanyId,
            CategoryId = _context.Categories.First().Id,
            StatusId = _context.Statuses.First().Id,
            CreatedById = _clienteId,
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(4)
        });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetTicketChatAsync(ticketId, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private AuditService CreateService()
    {
        TicketRepository ticketRepository = new(_context);
        ChatMessageRepository chatMessageRepository = new(_context);
        UserRepository userRepository = new(_context);
        FakeIdentityService identityService = new();
        FakeCurrentUserService currentUser = new(_adminId, _companyId);

        return new AuditService(ticketRepository, userRepository, identityService, currentUser, chatMessageRepository);
    }
}
