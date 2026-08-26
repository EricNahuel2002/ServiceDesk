using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class TicketFeedbackConfiguration : IEntityTypeConfiguration<TicketFeedback>
{
    public void Configure(EntityTypeBuilder<TicketFeedback> builder)
    {
        builder.Property(f => f.Comment)
            .HasMaxLength(2000);

        builder.HasIndex(f => f.TicketId);
        builder.HasIndex(f => f.ClientId);

        builder.HasOne(f => f.Ticket)
            .WithMany(t => t.Feedbacks)
            .HasForeignKey(f => f.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Client)
            .WithMany()
            .HasForeignKey(f => f.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
