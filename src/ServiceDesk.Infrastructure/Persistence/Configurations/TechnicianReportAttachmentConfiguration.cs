using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class TechnicianReportAttachmentConfiguration : IEntityTypeConfiguration<TechnicianReportAttachment>
{
    public void Configure(EntityTypeBuilder<TechnicianReportAttachment> builder)
    {
        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.BlobName)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(a => a.TechnicianReportId);

        builder.HasOne(a => a.TechnicianReport)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.TechnicianReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
