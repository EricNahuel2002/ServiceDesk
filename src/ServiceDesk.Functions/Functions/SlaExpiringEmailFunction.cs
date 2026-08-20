using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Functions.Functions;

public sealed class SlaExpiringEmailFunction
{
    private const string Schedule = "0 */5 * * * *";

    private readonly ISlaMonitoringService _slaMonitoring;
    private readonly IEmailService _email;
    private readonly ILogger<SlaExpiringEmailFunction> _logger;

    public SlaExpiringEmailFunction(
        ISlaMonitoringService slaMonitoring,
        IEmailService email,
        ILogger<SlaExpiringEmailFunction> logger)
    {
        _slaMonitoring = slaMonitoring;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(SlaExpiringEmailFunction))]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SlaExpiringNotification> notifications =
            await _slaMonitoring.GetTicketsPendingExpiringNotificationAsync(DateTime.UtcNow, cancellationToken);

        foreach (SlaExpiringNotification notification in notifications)
        {
            string subject = BuildSubject(notification);
            string body = BuildBody(notification);

            await _email.SendAsync(notification.TechnicianEmail, subject, body, cancellationToken);

            await _slaMonitoring.MarkExpiringNotifiedAsync([notification.TicketId], cancellationToken);

            _logger.LogInformation(
                "Notificación de SLA por vencer enviada al técnico del ticket {TicketId}.",
                notification.TicketId);
        }
    }

    internal static string BuildSubject(SlaExpiringNotification notification) =>
        $"SLA por vencer: {notification.Title}";

    internal static string BuildBody(SlaExpiringNotification notification) =>
        $"""
        Hola {notification.TechnicianFirstName},

        El SLA del siguiente ticket está por vencer:

        Título: {notification.Title}
        Prioridad: {notification.PriorityName}
        Vence: {notification.ResponseDeadlineAtUtc:yyyy-MM-dd HH:mm 'UTC'}

        Te recomendamos resolverlo antes del vencimiento para evitar el incumplimiento.

        Saludos,
        ServiceDesk
        """;
}