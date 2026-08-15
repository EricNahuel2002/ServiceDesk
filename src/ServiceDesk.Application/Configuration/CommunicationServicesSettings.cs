namespace ServiceDesk.Application.Configuration;

public sealed class CommunicationServicesSettings
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string SenderAddress { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = "ServiceDesk";
}
