using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence;

public class ServiceDeskDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork
{
    public ServiceDeskDbContext(DbContextOptions<ServiceDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Priority> Priorities => Set<Priority>();

    public DbSet<Status> Statuses => Set<Status>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceDeskDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        DateTime now = DateTime.UtcNow;

        foreach (EntityEntry<BaseEntity> entry in ChangeTracker.Entries<BaseEntity>())
        {
            UpdateAuditFields(entry, now);
        }

        foreach (EntityEntry<ApplicationUser> entry in ChangeTracker.Entries<ApplicationUser>())
        {
            UpdateAuditFields(entry, now);
        }
    }

    private static void UpdateAuditFields(EntityEntry entry, DateTime now)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Property(nameof(BaseEntity.CreatedAtUtc)).CurrentValue = now;
                break;
            case EntityState.Modified:
                entry.Property(nameof(BaseEntity.UpdatedAtUtc)).CurrentValue = now;
                break;
        }
    }
}
