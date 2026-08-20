using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Functions.Functions;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Functions;

public sealed class SlaBreachedEmailFunctionTests
{
    private readonly FakeSlaMonitoringService _slaMonitoring = new();
    private readonly FakeEmailService _email = new();
    private readonly SlaBreachedEmailFunction _function;

    public SlaBreachedEmailFunctionTests()
    {
        _function = new SlaBreachedEmailFunction(
            _slaMonitoring,
            _email,
            NullLogger<SlaBreachedEmailFunction>.Instance);
    }

    [Fact]
    public async Task Run_SendsEmailToTechnicianAndMarksNotified()
    {
        DateTime graceDeadline = DateTime.UtcNow.AddMinutes(30);
        SlaBreachedNotification notification = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "VPN caída",
            PriorityName = "Alta",
            TechnicianFirstName = "Ana",
            TechnicianEmail = "ana@servicedesk.local",
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddMinutes(-5),
            GraceDeadlineUtc = graceDeadline
        };
        _slaMonitoring.BreachedNotifications = [notification];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        FakeEmailService.SentEmail email = Assert.Single(_email.Sent);
        Assert.Equal(notification.TechnicianEmail, email.ToEmail);
        Assert.Contains(notification.Title, email.Subject);
        Assert.Contains(notification.TechnicianFirstName, email.Body);
        Assert.Contains(graceDeadline.ToString("yyyy-MM-dd HH:mm"), email.Body);
        Assert.Contains(notification.TicketId, _slaMonitoring.MarkedBreached);
    }

    [Fact]
    public async Task Run_AppliesGraceExpirationsBeforeDetection()
    {
        _slaMonitoring.BreachedNotifications = [];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        Assert.Equal(1, _slaMonitoring.GraceExpirationsApplied);
    }

    [Fact]
    public async Task Run_PropagatesEmailFailure()
    {
        SlaBreachedNotification notification = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "Sin acceso",
            TechnicianFirstName = "Ana",
            TechnicianEmail = "ana@servicedesk.local",
            GraceDeadlineUtc = DateTime.UtcNow.AddMinutes(30)
        };
        _slaMonitoring.BreachedNotifications = [notification];
        _email.ThrowOnSend = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_slaMonitoring.MarkedBreached);
    }
}