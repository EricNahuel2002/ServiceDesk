namespace ServiceDesk.Application.DTOs.Sla;

public sealed record UpdateBusinessHoursRequest
{
    public string BusinessHoursJson { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public bool UseBusinessHours { get; init; }
}
