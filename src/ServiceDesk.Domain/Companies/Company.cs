using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Sla;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Domain.Companies;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Users { get; set; } = [];

    public ICollection<Ticket> Tickets { get; set; } = [];

    public ICollection<Category> Categories { get; set; } = [];

    public ICollection<Status> Statuses { get; set; } = [];

    public ICollection<AuditLog> AuditLogs { get; set; } = [];

    public ICollection<SlaConfiguration> SlaConfigurations { get; set; } = [];

    public CompanyBusinessHours? BusinessHours { get; set; }
}
