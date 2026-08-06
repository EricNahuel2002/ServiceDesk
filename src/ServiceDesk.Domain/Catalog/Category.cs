using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Domain.Catalog;

public class Category : BaseEntity
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = [];
}
