using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Functions.Functions;

public sealed class SlaCanceledEmailFunction
{
    private const string Schedule = "0 */5 * * * *";

    private readonly ISlaMonitoringService _slaMonitoring;
    private readonly IEmailService _email;
    private readonly ILogger<SlaCanceledEmailFunction> _logger;

    public SlaCanceledEmailFunction(
        ISlaMonitoringService slaMonitoring,
        IEmailService email,
        ILogger<SlaCanceledEmailFunction> logger)
    {
        _slaMonitoring = slaMonitoring;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(SlaCanceledEmailFunction))]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SlaCanceledNotification> notifications =
            await _slaMonitoring.GetPendingSlaCanceledNotificationsAsync(cancellationToken);

        foreach (SlaCanceledNotification notification in notifications)
        {
            string subject = BuildSubject(notification);
            string body = BuildBody(notification);

            await _email.SendAsync(notification.TechnicianEmail, subject, body, cancellationToken);

            await _slaMonitoring.MarkSlaCanceledNotifiedAsync([notification.RecordId], cancellationToken);

            _logger.LogInformation(
                "Notificación de participación cancelada enviada al técnico del ticket {TicketId}.",
                notification.TicketId);
        }
    }

    internal static string BuildSubject(SlaCanceledNotification notification) =>
        $"Se canceló tu participación en el ticket: {notification.Title}";

    internal static string BuildBody(SlaCanceledNotification notification) =>
        $"""
        Hola {notification.TechnicianFirstName},

        Te informamos que se canceló tu participación en el siguiente ticket por incumplimiento del SLA y vencimiento de la hora extra:

        Título: {notification.Title}
        Prioridad: {notification.PriorityName}
        Fecha de cancelación: {notification.CanceledAtUtc:yyyy-MM-dd HH:mm 'UTC'}

        El ticket quedó pendiente de una nueva asignación de técnico.

        Saludos,
        ServiceDesk
        """;
}