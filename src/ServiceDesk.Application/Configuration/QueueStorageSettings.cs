using ServiceDesk.Application.DTOs.Notifications;

namespace ServiceDesk.Application.Configuration;

public sealed class QueueStorageSettings
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = NotificationQueues.TicketAssigned;

    public string ClientNotificationQueueName { get; set; } = NotificationQueues.TicketAssignedToClient;

    public string ClientWorkNotificationQueueName { get; set; } = NotificationQueues.ClientWorkNotifications;
}
