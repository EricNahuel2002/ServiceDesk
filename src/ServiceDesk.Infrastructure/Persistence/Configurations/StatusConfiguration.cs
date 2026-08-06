using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => new { s.CompanyId, s.Name })
            .IsUnique();

        builder.HasOne(s => s.Company)
            .WithMany(company => company.Statuses)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
