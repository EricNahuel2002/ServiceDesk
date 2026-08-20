using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Functions.Functions;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Functions;

public sealed class SlaExpiringEmailFunctionTests
{
    private readonly FakeSlaMonitoringService _slaMonitoring = new();
    private readonly FakeEmailService _email = new();
    private readonly SlaExpiringEmailFunction _function;

    public SlaExpiringEmailFunctionTests()
    {
        _function = new SlaExpiringEmailFunction(
            _slaMonitoring,
            _email,
            NullLogger<SlaExpiringEmailFunction>.Instance);
    }

    [Fact]
    public async Task Run_SendsEmailToTechnicianAndMarksNotified()
    {
        SlaExpiringNotification notification = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "Equipo sin red",
            PriorityName = "Media",
            TechnicianFirstName = "Juan",
            TechnicianEmail = "juan@servicedesk.local",
            ResponseDeadlineAtUtc = DateTime.UtcNow.AddHours(1)
        };
        _slaMonitoring.ExpiringNotifications = [notification];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        FakeEmailService.SentEmail email = Assert.Single(_email.Sent);
        Assert.Equal(notification.TechnicianEmail, email.ToEmail);
        Assert.Contains(notification.Title, email.Subject);
        Assert.Contains(notification.TechnicianFirstName, email.Body);
        Assert.Contains(notification.PriorityName, email.Body);
        Assert.Contains(notification.TicketId, _slaMonitoring.MarkedExpiring);
    }

    [Fact]
    public async Task Run_SendsNothingWhenNoNotifications()
    {
        _slaMonitoring.ExpiringNotifications = [];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        Assert.Empty(_email.Sent);
        Assert.Empty(_slaMonitoring.MarkedExpiring);
    }

    [Fact]
    public async Task Run_PropagatesEmailFailure()
    {
        SlaExpiringNotification notification = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "Sin acceso",
            TechnicianFirstName = "Ana",
            TechnicianEmail = "ana@servicedesk.local"
        };
        _slaMonitoring.ExpiringNotifications = [notification];
        _email.ThrowOnSend = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_slaMonitoring.MarkedExpiring);
    }

    [Fact]
    public async Task Run_PropagatesMonitoringFailure()
    {
        _slaMonitoring.ThrowOnExpiring = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_email.Sent);
    }
}