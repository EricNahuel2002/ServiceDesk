using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Audit;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }

    public Guid CompanyId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Details { get; set; }

    public ApplicationUser? User { get; set; }

    public Company? Company { get; set; }
}
