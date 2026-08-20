using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;

namespace ServiceDesk.Domain.Sla;

public class CompanyBusinessHours : BaseEntity
{
    public Guid CompanyId { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public string BusinessHoursJson { get; set; } = string.Empty;

    public bool UseBusinessHours { get; set; } = true;

    public int MaxAssignmentToStartMinutes { get; set; }

    public Company? Company { get; set; }
}
