using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class TicketSlaRecordConfiguration : IEntityTypeConfiguration<TicketSlaRecord>
{
    public void Configure(EntityTypeBuilder<TicketSlaRecord> builder)
    {
        builder.Property(r => r.Priority)
            .HasConversion<int>();

        builder.HasIndex(r => r.TicketId);
        builder.HasIndex(r => r.TechnicianId);
        builder.HasIndex(r => r.IsCurrent);

        builder.HasOne(r => r.Ticket)
            .WithMany(t => t.SlaRecords)
            .HasForeignKey(r => r.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}