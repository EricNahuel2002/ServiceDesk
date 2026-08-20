using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Functions.Functions;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Functions;

public sealed class SlaCanceledEmailFunctionTests
{
    private readonly FakeSlaMonitoringService _slaMonitoring = new();
    private readonly FakeEmailService _email = new();
    private readonly SlaCanceledEmailFunction _function;

    public SlaCanceledEmailFunctionTests()
    {
        _function = new SlaCanceledEmailFunction(
            _slaMonitoring,
            _email,
            NullLogger<SlaCanceledEmailFunction>.Instance);
    }

    [Fact]
    public async Task Run_SendsEmailToTechnicianAndMarksNotified()
    {
        SlaCanceledNotification notification = new()
        {
            RecordId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            TechnicianId = Guid.NewGuid(),
            Title = "Impresora rota",
            PriorityName = "Media",
            TechnicianFirstName = "Luis",
            TechnicianEmail = "luis@servicedesk.local",
            CanceledAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        _slaMonitoring.CanceledNotifications = [notification];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        FakeEmailService.SentEmail email = Assert.Single(_email.Sent);
        Assert.Equal(notification.TechnicianEmail, email.ToEmail);
        Assert.Contains(notification.Title, email.Subject);
        Assert.Contains(notification.TechnicianFirstName, email.Body);
        Assert.Contains(notification.RecordId, _slaMonitoring.MarkedCanceled);
    }

    [Fact]
    public async Task Run_SendsNothingWhenNoNotifications()
    {
        _slaMonitoring.CanceledNotifications = [];

        await _function.Run(new TimerInfo(), CancellationToken.None);

        Assert.Empty(_email.Sent);
        Assert.Empty(_slaMonitoring.MarkedCanceled);
    }

    [Fact]
    public async Task Run_PropagatesEmailFailure()
    {
        SlaCanceledNotification notification = new()
        {
            RecordId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            TechnicianId = Guid.NewGuid(),
            Title = "Sin acceso",
            TechnicianFirstName = "Luis",
            TechnicianEmail = "luis@servicedesk.local"
        };
        _slaMonitoring.CanceledNotifications = [notification];
        _email.ThrowOnSend = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_slaMonitoring.MarkedCanceled);
    }

    [Fact]
    public async Task Run_PropagatesMonitoringFailure()
    {
        _slaMonitoring.ThrowOnCanceled = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(new TimerInfo(), CancellationToken.None));

        Assert.Empty(_email.Sent);
    }
}