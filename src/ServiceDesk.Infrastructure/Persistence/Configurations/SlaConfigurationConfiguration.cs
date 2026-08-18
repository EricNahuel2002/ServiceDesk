using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class SlaConfigurationConfiguration : IEntityTypeConfiguration<SlaConfiguration>
{
    public void Configure(EntityTypeBuilder<SlaConfiguration> builder)
    {
        builder.HasIndex(s => new { s.CompanyId, s.Priority })
            .IsUnique();

        builder.Property(s => s.ResponseTimeHours)
            .IsRequired();

        builder.HasOne(s => s.Company)
            .WithMany(c => c.SlaConfigurations)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
