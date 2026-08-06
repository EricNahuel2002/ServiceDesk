using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class PriorityConfiguration : IEntityTypeConfiguration<Priority>
{
    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => new { p.CompanyId, p.Name })
            .IsUnique();

        builder.HasOne(p => p.Company)
            .WithMany(company => company.Priorities)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
