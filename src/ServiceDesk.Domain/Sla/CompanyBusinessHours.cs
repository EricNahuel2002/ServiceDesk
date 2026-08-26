using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;

namespace ServiceDesk.Domain.Sla;

public class CompanyBusinessHours : BaseEntity
{
    public Guid CompanyId { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public WeeklySchedule Schedule { get; set; } = WeeklySchedule.CreateDefault();

    public bool UseBusinessHours { get; set; } = true;

    public int MaxAssignmentToStartMinutes { get; set; }

    public Company? Company { get; set; }
}
