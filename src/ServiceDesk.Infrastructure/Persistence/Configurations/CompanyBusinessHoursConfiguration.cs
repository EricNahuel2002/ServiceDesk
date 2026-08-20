using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Sla;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class CompanyBusinessHoursConfiguration : IEntityTypeConfiguration<CompanyBusinessHours>
{
    public void Configure(EntityTypeBuilder<CompanyBusinessHours> builder)
    {
        builder.HasIndex(b => b.CompanyId)
            .IsUnique();

        builder.Property(b => b.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.BusinessHoursJson)
            .IsRequired();

        builder.HasOne(b => b.Company)
            .WithOne(c => c.BusinessHours)
            .HasForeignKey<CompanyBusinessHours>(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
