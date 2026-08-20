using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Functions.Functions;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Functions;

public sealed class AdminReassignmentEmailFunctionTests
{
    private readonly FakeSlaMonitoringService _slaMonitoring = new();
    private readonly FakeEmailService _email = new();
    private readonly AdminReassignmentEmailFunction _function;

    public AdminReassignmentEmailFunctionTests()
    {
        _function = new AdminReassignmentEmailFunction(
            _slaMonitoring,
            _email,
            NullLogger<AdminReassignmentEmailFunction>.Instance);
    }

    [Fact]
    public async Task Run_SendsEmailToEachAdminAndMarksNotified()
    {
        AdminReassignmentNotification notification = new()
        {
            RecordId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            Title = "Impresora rota",
            PriorityName = "Media",
            TechnicianFirstName = "Luis",
            TechnicianLastName = "Pérez",
            AssignedAtUtc = DateTime.UtcNow.AddMinutes(-200),
            StartGraceDeadlineUtc = DateTime.UtcNow.AddMinutes(-50),
            AdminEmails = ["ana@servicedesk.local", "carlos@servicedesk.local"]
        };
        _slaMonitoring.AdminReassignmentNotifications = [notification];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        Assert.Equal(1, _slaMonitoring.AssignmentStartGraceExpirationsApplied);
        Assert.Equal(2, _email.Sent.Count);
        Assert.Equal("ana@servicedesk.local", _email.Sent[0].ToEmail);
        Assert.Equal("carlos@servicedesk.local", _email.Sent[1].ToEmail);
        Assert.Contains(notification.Title, _email.Sent[0].Subject);
        Assert.Contains(notification.TechnicianFirstName, _email.Sent[0].Body);
        Assert.Contains(notification.RecordId, _slaMonitoring.MarkedAdminReassignment);
    }

    [Fact]
    public async Task Run_SendsNothingWhenNoNotifications()
    {
        _slaMonitoring.AdminReassignmentNotifications = [];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        Assert.Equal(1, _slaMonitoring.AssignmentStartGraceExpirationsApplied);
        Assert.Empty(_email.Sent);
        Assert.Empty(_slaMonitoring.MarkedAdminReassignment);
    }

    [Fact]
    public async Task Run_PropagatesEmailFailure()
    {
        AdminReassignmentNotification notification = new()
        {
            RecordId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            Title = "Sin acceso",
            TechnicianFirstName = "Luis",
            AdminEmails = ["ana@servicedesk.local"]
        };
        _slaMonitoring.AdminReassignmentNotifications = [notification];
        _email.ThrowOnSend = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_slaMonitoring.MarkedAdminReassignment);
    }

    [Fact]
    public async Task Run_PropagatesMonitoringFailure()
    {
        _slaMonitoring.ThrowOnAdminReassignment = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_email.Sent);
    }
}