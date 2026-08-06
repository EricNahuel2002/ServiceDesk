using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasData(
            new ApplicationRole
            {
                Id = Guid.Parse("3E53C9D6-7B4A-4F0E-9A1B-2C3D4E5F6A7B"),
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR"
            },
            new ApplicationRole
            {
                Id = Guid.Parse("4F64D0E7-8C5B-4A1F-8B2C-3D4E5F6A7B8C"),
                Name = "Technician",
                NormalizedName = "TECHNICIAN"
            },
            new ApplicationRole
            {
                Id = Guid.Parse("5A75E1F8-9D6C-4B2A-9C3D-4E5F6A7B8C9D"),
                Name = "Client",
                NormalizedName = "CLIENT"
            });
    }
}
