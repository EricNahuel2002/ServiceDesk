namespace ServiceDesk.Application.Configuration;

public sealed class QueueStorageSettings
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "ticket-notifications";
}
