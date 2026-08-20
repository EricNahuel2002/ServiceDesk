using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Functions.Functions;

public sealed class SlaBreachedEmailFunction
{
    private const string Schedule = "0 */5 * * * *";

    private readonly ISlaMonitoringService _slaMonitoring;
    private readonly IEmailService _email;
    private readonly ILogger<SlaBreachedEmailFunction> _logger;

    public SlaBreachedEmailFunction(
        ISlaMonitoringService slaMonitoring,
        IEmailService email,
        ILogger<SlaBreachedEmailFunction> logger)
    {
        _slaMonitoring = slaMonitoring;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(SlaBreachedEmailFunction))]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        await _slaMonitoring.ApplySlaGraceExpirationsAsync(now, cancellationToken);

        IReadOnlyList<SlaBreachedNotification> notifications =
            await _slaMonitoring.GetTicketsPendingBreachNotificationAsync(now, cancellationToken);

        foreach (SlaBreachedNotification notification in notifications)
        {
            string subject = BuildSubject(notification);
            string body = BuildBody(notification);

            await _email.SendAsync(notification.TechnicianEmail, subject, body, cancellationToken);

            await _slaMonitoring.MarkBreachedNotifiedAsync([notification.TicketId], cancellationToken);

            _logger.LogInformation(
                "Notificación de SLA incumplido enviada al técnico del ticket {TicketId}.",
                notification.TicketId);
        }
    }

    internal static string BuildSubject(SlaBreachedNotification notification) =>
        $"SLA incumplido: {notification.Title}";

    internal static string BuildBody(SlaBreachedNotification notification) =>
        $"""
        Hola {notification.TechnicianFirstName},

        El SLA del siguiente ticket fue incumplido:

        Título: {notification.Title}
        Prioridad: {notification.PriorityName}
        Vencía: {notification.ResponseDeadlineAtUtc:yyyy-MM-dd HH:mm 'UTC'}

        Tenés 1 hora extra para finalizarlo. La hora extra vence el {notification.GraceDeadlineUtc:yyyy-MM-dd HH:mm 'UTC'}.
        Si no lo finalizás antes, se cancelará tu participación en el ticket y será reasignado a otro técnico.

        Saludos,
        ServiceDesk
        """;
}