using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Sla;

public class SlaConfiguration : BaseEntity
{
    public Guid CompanyId { get; set; }

    public TicketPriority Priority { get; set; }

    public int ResponseTimeHours { get; set; }

    public Company? Company { get; set; }
}
