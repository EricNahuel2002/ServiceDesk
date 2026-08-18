using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Functions.Functions;
using ServiceDesk.UnitTests.Fakes;

namespace ServiceDesk.UnitTests.Functions;

public sealed class WorkStartedEmailFunctionTests
{
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeEmailService _email = new();
    private readonly WorkStartedEmailFunction _function;

    public WorkStartedEmailFunctionTests()
    {
        _function = new WorkStartedEmailFunction(
            _tickets,
            _email,
            NullLogger<WorkStartedEmailFunction>.Instance);
    }

    [Fact]
    public async Task Run_SendsEmailToRequester()
    {
        TicketNotificationInfo info = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "PC sin encender",
            Description = "La PC no enciende",
            PriorityName = "Alta",
            AssignedToFirstName = "Juan",
            AssignedToLastName = "Pérez",
            AssignedToEmail = "juan@servicedesk.local",
            RequesterFirstName = "María",
            RequesterEmail = "maria@servicedesk.local"
        };
        _tickets.NotificationInfo = info;
        string message = Serialize(new TicketAssignedNotification
        {
            EventType = NotificationEvents.WorkStarted,
            TicketId = info.TicketId
        });

        await _function.Run(message, CancellationToken.None);

        FakeEmailService.SentEmail email = Assert.Single(_email.Sent);
        Assert.Equal(info.RequesterEmail, email.ToEmail);
        Assert.Contains(info.Title, email.Subject);
        Assert.Contains(info.RequesterFirstName, email.Body);
        Assert.Contains(info.AssignedToFirstName, email.Body);
    }

    [Fact]
    public async Task Run_IgnoresUnknownEventType()
    {
        _tickets.NotificationInfo = new TicketNotificationInfo();
        string message = Serialize(new TicketAssignedNotification
        {
            EventType = "WorkFinished",
            TicketId = Guid.NewGuid()
        });

        await _function.Run(message, CancellationToken.None);

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Run_SkipsWhenTicketHasNoTechnicianAssigned()
    {
        _tickets.NotificationInfo = null;
        string message = Serialize(new TicketAssignedNotification
        {
            EventType = NotificationEvents.WorkStarted,
            TicketId = Guid.NewGuid()
        });

        await _function.Run(message, CancellationToken.None);

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Run_PropagatesEmailFailure()
    {
        TicketNotificationInfo info = new()
        {
            TicketId = Guid.NewGuid(),
            Title = "Sin acceso al sistema",
            PriorityName = "Media",
            AssignedToFirstName = "Ana",
            RequesterEmail = "maria@servicedesk.local"
        };
        _tickets.NotificationInfo = info;
        _email.ThrowOnSend = true;
        string message = Serialize(new TicketAssignedNotification
        {
            EventType = NotificationEvents.WorkStarted,
            TicketId = info.TicketId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run(message, CancellationToken.None));
    }

    [Fact]
    public async Task Run_ThrowsJsonException_WhenMessageIsNotValidJson()
    {
        await Assert.ThrowsAsync<JsonException>(() =>
            _function.Run("no-es-json", CancellationToken.None));
    }

    [Fact]
    public async Task Run_ThrowsInvalidOperationException_WhenMessageDeserializesToNull()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _function.Run("null", CancellationToken.None));
    }

    private static string Serialize(TicketAssignedNotification notification) =>
        JsonSerializer.Serialize(notification);
}
