namespace ServiceDesk.Application.Configuration;

public sealed class BlobStorageSettings
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "ticketsattachments";
}
