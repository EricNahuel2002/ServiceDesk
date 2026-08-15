using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Functions.Functions;

public sealed class TicketAssignedEmailFunction
{
    private const string TicketAssignedEvent = "TicketAssigned";

    private readonly ITicketRepository _tickets;
    private readonly IEmailService _email;
    private readonly ILogger<TicketAssignedEmailFunction> _logger;

    public TicketAssignedEmailFunction(
        ITicketRepository tickets,
        IEmailService email,
        ILogger<TicketAssignedEmailFunction> logger)
    {
        _tickets = tickets;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(TicketAssignedEmailFunction))]
    public async Task Run(
        [QueueTrigger("%QueueName%")] string message,
        CancellationToken cancellationToken)
    {
        TicketAssignedNotification notification = Deserialize(message);

        if (!string.Equals(notification.EventType, TicketAssignedEvent, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Se ignoró el evento {EventType} de la cola de notificaciones.",
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

        await _email.SendAsync(info.AssignedToEmail, subject, body, cancellationToken);
    }

    private static TicketAssignedNotification Deserialize(string message)
    {
        TicketAssignedNotification? notification =
            JsonSerializer.Deserialize<TicketAssignedNotification>(message);

        return notification ?? throw new InvalidOperationException(
            "El mensaje de la cola no contiene una notificación válida.");
    }

    internal static string BuildSubject(TicketNotificationInfo info) =>
        $"Se te asignó un ticket: {info.Title}";

    internal static string BuildBody(TicketNotificationInfo info) =>
        $"""
        Hola {info.AssignedToFirstName},

        Se te asignó el siguiente ticket:

        Título: {info.Title}
        Descripción: {info.Description}
        Prioridad: {info.PriorityName}

        Saludos,
        ServiceDesk
        """;
}
