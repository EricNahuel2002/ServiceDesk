using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.Features.Sla;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Sla;

public sealed class SlaMonitoringServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly SlaMonitoringService _service;

    public SlaMonitoringServiceTests()
    {
        _service = new SlaMonitoringService(_tickets, _catalog, _users, _unitOfWork);
    }

    [Fact]
    public async Task GetTicketsPendingExpiringNotificationAsync_ReturnsExpiringTicket()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Media, Now.AddHours(2));
        TicketSlaRecord record = CreateRecord(ticket, 8);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Juan", "juan@servicedesk.local", "Equipo sin red");

        IReadOnlyList<SlaExpiringNotification> notifications =
            await _service.GetTicketsPendingExpiringNotificationAsync(Now, CancellationToken.None);

        SlaExpiringNotification notification = Assert.Single(notifications);
        Assert.Equal(ticket.Id, notification.TicketId);
        Assert.Equal("juan@servicedesk.local", notification.TechnicianEmail);
        Assert.Equal("Equipo sin red", notification.Title);
        Assert.Null(record.ExpiringNotifiedAtUtc);
    }

    [Fact]
    public async Task GetTicketsPendingExpiringNotificationAsync_ExcludesAlreadyNotified()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Media, Now.AddHours(2));
        TicketSlaRecord record = CreateRecord(ticket, 8);
        record.ExpiringNotifiedAtUtc = Now.AddMinutes(-5);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Juan", "juan@servicedesk.local", "Equipo sin red");

        IReadOnlyList<SlaExpiringNotification> notifications =
            await _service.GetTicketsPendingExpiringNotificationAsync(Now, CancellationToken.None);

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetTicketsPendingExpiringNotificationAsync_ExcludesBreachedTicket()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Media, Now.AddHours(2));
        TicketSlaRecord record = CreateRecord(ticket, 8);
        record.BreachedAtUtc = Now.AddMinutes(-10);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Juan", "juan@servicedesk.local", "Equipo sin red");

        IReadOnlyList<SlaExpiringNotification> notifications =
            await _service.GetTicketsPendingExpiringNotificationAsync(Now, CancellationToken.None);

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetTicketsPendingBreachNotificationAsync_MarksBreachAndReturnsNotification()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Alta, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Ana", "ana@servicedesk.local", "VPN caída");

        IReadOnlyList<SlaBreachedNotification> notifications =
            await _service.GetTicketsPendingBreachNotificationAsync(Now, CancellationToken.None);

        SlaBreachedNotification notification = Assert.Single(notifications);
        Assert.Equal(ticket.Id, notification.TicketId);
        Assert.Equal(Now, record.BreachedAtUtc);
        Assert.Equal(Now.AddMinutes(SlaPolicy.GraceMinutes), record.GraceDeadlineUtc);
        Assert.Equal(record.GraceDeadlineUtc, notification.GraceDeadlineUtc);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetTicketsPendingBreachNotificationAsync_DoesNotReturnAgainAfterMarkedNotified()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Alta, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Ana", "ana@servicedesk.local", "VPN caída");

        _ = await _service.GetTicketsPendingBreachNotificationAsync(Now, CancellationToken.None);
        await _service.MarkBreachedNotifiedAsync([ticket.Id], CancellationToken.None);

        IReadOnlyList<SlaBreachedNotification> notifications =
            await _service.GetTicketsPendingBreachNotificationAsync(Now, CancellationToken.None);

        Assert.Empty(notifications);
        Assert.NotNull(record.BreachedNotifiedAtUtc);
    }

    [Fact]
    public async Task GetTicketsPendingBreachNotificationAsync_ReturnsPendingNotificationWhenNotNotified()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-6), TicketPriority.Alta, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        record.BreachedAtUtc = Now.AddHours(-1);
        record.GraceDeadlineUtc = Now;
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _tickets.NotificationInfo = BuildNotificationInfo(ticket.Id, "Ana", "ana@servicedesk.local", "VPN caída");

        IReadOnlyList<SlaBreachedNotification> notifications =
            await _service.GetTicketsPendingBreachNotificationAsync(Now, CancellationToken.None);

        Assert.Single(notifications);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ApplySlaGraceExpirationsAsync_UnassignsAndCancelsRecord()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-8), TicketPriority.Alta, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 2);
        record.BreachedAtUtc = Now.AddHours(-2);
        record.GraceDeadlineUtc = Now.AddHours(-1);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _catalog.NuevoStatusId = Guid.NewGuid();

        await _service.ApplySlaGraceExpirationsAsync(Now, CancellationToken.None);

        Assert.Equal(Now, record.CanceledAtUtc);
        Assert.False(record.IsCurrent);
        Assert.Null(ticket.AssignedToId);
        Assert.Equal(_catalog.NuevoStatusId, ticket.StatusId);
        TicketComment comment = Assert.Single(_tickets.Comments);
        Assert.Null(comment.AuthorId);
        Assert.True(comment.IsInternal);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ApplySlaGraceExpirationsAsync_DoesNothingWithinGracePeriod()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-8), TicketPriority.Alta, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 2);
        record.BreachedAtUtc = Now.AddMinutes(-30);
        record.GraceDeadlineUtc = Now.AddMinutes(30);
        _tickets.SlaTickets.Add(ticket);
        _tickets.SlaRecords.Add(record);
        ticket.SlaRecords.Add(record);
        _catalog.NuevoStatusId = Guid.NewGuid();

        await _service.ApplySlaGraceExpirationsAsync(Now, CancellationToken.None);

        Assert.Null(record.CanceledAtUtc);
        Assert.NotNull(ticket.AssignedToId);
        Assert.Empty(_tickets.Comments);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetPendingSlaCanceledNotificationsAsync_ReturnsNotificationForCanceledRecord()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-8), TicketPriority.Media, Now.AddHours(-2));
        ticket.Title = "Impresora rota";
        TicketSlaRecord record = CreateRecord(ticket, 4);
        record.BreachedAtUtc = Now.AddHours(-2);
        record.GraceDeadlineUtc = Now.AddHours(-1);
        record.CanceledAtUtc = Now.AddMinutes(-5);
        record.Ticket = ticket;
        _tickets.SlaRecords.Add(record);

        _users.User = new ApplicationUser
        {
            Id = record.TechnicianId!.Value,
            FirstName = "Luis",
            Email = "luis@servicedesk.local"
        };

        IReadOnlyList<SlaCanceledNotification> notifications =
            await _service.GetPendingSlaCanceledNotificationsAsync(CancellationToken.None);

        SlaCanceledNotification notification = Assert.Single(notifications);
        Assert.Equal(record.Id, notification.RecordId);
        Assert.Equal("luis@servicedesk.local", notification.TechnicianEmail);
        Assert.Equal("Impresora rota", notification.Title);
    }

    [Fact]
    public async Task MarkSlaCanceledNotifiedAsync_SetsMarker()
    {
        Ticket ticket = CreateTicket(Now.AddHours(-8), TicketPriority.Media, Now.AddHours(-2));
        TicketSlaRecord record = CreateRecord(ticket, 4);
        record.CanceledAtUtc = Now.AddMinutes(-5);
        _tickets.SlaRecords.Add(record);

        await _service.MarkSlaCanceledNotifiedAsync([record.Id], CancellationToken.None);

        Assert.NotNull(record.CanceledNotifiedAtUtc);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    private static Ticket CreateTicket(DateTime createdAt, TicketPriority priority, DateTime deadline)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            CreatedAtUtc = createdAt,
            Priority = priority,
            ResponseDeadlineAtUtc = deadline,
            AssignedToId = Guid.NewGuid(),
            Title = "Ticket de prueba"
        };
    }

    private static TicketSlaRecord CreateRecord(Ticket ticket, int slaLimitHours)
    {
        return new TicketSlaRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            TechnicianId = ticket.AssignedToId,
            Priority = ticket.Priority!.Value,
            SlaLimitHours = slaLimitHours,
            ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc,
            IsCurrent = true
        };
    }

    private static TicketNotificationInfo BuildNotificationInfo(
        Guid ticketId,
        string firstName,
        string email,
        string title) =>
        new()
        {
            TicketId = ticketId,
            Title = title,
            PriorityName = "Alta",
            AssignedToFirstName = firstName,
            AssignedToEmail = email
        };
}