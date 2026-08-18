using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Sla;

public sealed record UpdateSlaConfigurationRequest
{
    public TicketPriority Priority { get; init; }

    public int ResponseTimeHours { get; init; }
}
