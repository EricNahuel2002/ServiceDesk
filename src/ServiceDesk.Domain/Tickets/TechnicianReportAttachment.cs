using ServiceDesk.Domain.Common;

namespace ServiceDesk.Domain.Tickets;

public class TechnicianReportAttachment : BaseEntity
{
    public Guid TechnicianReportId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public TechnicianReport? TechnicianReport { get; set; }
}
