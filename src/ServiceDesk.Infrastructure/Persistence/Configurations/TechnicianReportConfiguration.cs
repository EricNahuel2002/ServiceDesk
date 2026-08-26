using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class TechnicianReportConfiguration : IEntityTypeConfiguration<TechnicianReport>
{
    public void Configure(EntityTypeBuilder<TechnicianReport> builder)
    {
        builder.Property(r => r.Reason)
            .HasMaxLength(2000);

        builder.HasIndex(r => r.TicketId);
        builder.HasIndex(r => r.TechnicianId);

        builder.HasOne(r => r.Ticket)
            .WithMany(t => t.TechnicianReports)
            .HasForeignKey(r => r.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReportedBy)
            .WithMany()
            .HasForeignKey(r => r.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Technician)
            .WithMany()
            .HasForeignKey(r => r.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
