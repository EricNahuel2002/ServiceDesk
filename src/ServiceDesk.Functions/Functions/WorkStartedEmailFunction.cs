using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Functions.Functions;

public sealed class WorkStartedEmailFunction
{
    private readonly ITicketRepository _tickets;
    private readonly IEmailService _email;
    private readonly ILogger<WorkStartedEmailFunction> _logger;

    public WorkStartedEmailFunction(
        ITicketRepository tickets,
        IEmailService email,
        ILogger<WorkStartedEmailFunction> logger)
    {
        _tickets = tickets;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(WorkStartedEmailFunction))]
    public async Task Run(
        [QueueTrigger(NotificationQueues.ClientWorkNotifications)] string message,
        CancellationToken cancellationToken)
    {
        TicketAssignedNotification notification = Deserialize(message);

        if (!string.Equals(notification.EventType, NotificationEvents.WorkStarted, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Se ignoró el evento {EventType} de la cola de notificaciones de trabajo.",
                notification.EventType);

            return;
        }

        TicketNotificationInfo? info = await _tickets.GetTicketNotificationInfoAsync(
            notification.TicketId,
            cancellationToken);

        if (info is null)
        {
            _logger.LogWarning(
                "No se encontró el ticket {TicketId} o no tiene un técnico asignado; se omite la notificación.",
                notification.TicketId);

            return;
        }

        string subject = BuildSubject(info);
        string body = BuildBody(info);

        await _email.SendAsync(info.RequesterEmail, subject, body, cancellationToken);
    }

    private static TicketAssignedNotification Deserialize(string message)
    {
        TicketAssignedNotification? notification =
            JsonSerializer.Deserialize<TicketAssignedNotification>(message);

        return notification ?? throw new InvalidOperationException(
            "El mensaje de la cola no contiene una notificación válida.");
    }

    internal static string BuildSubject(TicketNotificationInfo info) =>
        $"El técnico comenzó a trabajar en tu ticket: {info.Title}";

    internal static string BuildBody(TicketNotificationInfo info) =>
        $"""
        Hola {info.RequesterFirstName},

        Te informamos que el técnico {info.AssignedToFirstName} {info.AssignedToLastName}
        comenzó a trabajar en tu ticket "{info.Title}".

        Si necesitás más información, podés contactar al administrador de tu empresa.

        Saludos,
        ServiceDesk
        """;
}
