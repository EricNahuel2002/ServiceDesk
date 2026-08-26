using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Functions.Functions;

public sealed class AdminReassignmentEmailFunction
{
    private const string Schedule = "0 */5 * * * *";

    private readonly ISlaMonitoringService _slaMonitoring;
    private readonly IEmailService _email;
    private readonly ILogger<AdminReassignmentEmailFunction> _logger;

    public AdminReassignmentEmailFunction(
        ISlaMonitoringService slaMonitoring,
        IEmailService email,
        ILogger<AdminReassignmentEmailFunction> logger)
    {
        _slaMonitoring = slaMonitoring;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(AdminReassignmentEmailFunction))]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        await _slaMonitoring.ApplyAssignmentStartGraceExpirationsAsync(now, cancellationToken);

        IReadOnlyList<AdminReassignmentNotification> notifications =
            await _slaMonitoring.GetPendingAdminReassignmentNotificationsAsync(cancellationToken);

        foreach (AdminReassignmentNotification notification in notifications)
        {
            string subject = BuildSubject(notification);
            string body = BuildBody(notification);

            foreach (string adminEmail in notification.AdminEmails)
            {
                await _email.SendAsync(adminEmail, subject, body, cancellationToken);
            }

            await _slaMonitoring.MarkAdminReassignmentNotifiedAsync([notification.RecordId], cancellationToken);

            _logger.LogInformation(
                "Notificación de reasignación pendiente enviada a los administradores del ticket {TicketId}.",
                notification.TicketId);
        }
    }

    internal static string BuildSubject(AdminReassignmentNotification notification) =>
        notification.CancelReason == SlaRecordCancelReason.ReopenedByClientFeedback
            ? $"El cliente solicitó reasignación: ticket {notification.Title}"
            : $"Técnico sin respuesta: ticket {notification.Title}";

    internal static string BuildBody(AdminReassignmentNotification notification) =>
        notification.CancelReason == SlaRecordCancelReason.ReopenedByClientFeedback
            ? $"""
              Hola,

              El cliente indicó que su problema no fue resuelto en el siguiente ticket
              y solicitó la reasignación de un técnico:

              Título: {notification.Title}
              Prioridad: {notification.PriorityName}
              Técnico anterior: {notification.TechnicianFirstName} {notification.TechnicianLastName}
              Asignado el: {notification.AssignedAtUtc:yyyy-MM-dd HH:mm 'UTC'}

              El ticket fue devuelto al estado 'Nuevo' y queda pendiente de asignación.
              Ingresá al panel de administración para asignar un nuevo técnico.

              Saludos,
              ServiceDesk
              """
            : $"""
              Hola,

              El técnico asignado al siguiente ticket no inició el trabajo dentro del tiempo máximo permitido
              más el tiempo de gracia de 30 minutos:

              Título: {notification.Title}
              Prioridad: {notification.PriorityName}
              Técnico asignado: {notification.TechnicianFirstName} {notification.TechnicianLastName}
              Asignado el: {notification.AssignedAtUtc:yyyy-MM-dd HH:mm 'UTC'}
              Debía iniciar antes de: {notification.StartGraceDeadlineUtc:yyyy-MM-dd HH:mm 'UTC'}

              El ticket fue devuelto al estado 'Nuevo' y queda pendiente de asignación.
              Ingresá al panel de administración para asignar un nuevo técnico.

              Saludos,
              ServiceDesk
              """;
}
