using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Sla;

public sealed record SlaConfigurationDto
{
    public TicketPriority Priority { get; init; }

    public int ResponseTimeHours { get; init; }
}
