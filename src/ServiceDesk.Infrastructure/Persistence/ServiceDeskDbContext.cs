using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Sla;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence;

public class ServiceDeskDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork
{
    private static readonly DateTimeUtcInterceptor _dateTimeUtcInterceptor = new();

    public ServiceDeskDbContext(DbContextOptions<ServiceDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    public DbSet<TicketSlaRecord> TicketSlaRecords => Set<TicketSlaRecord>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Status> Statuses => Set<Status>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<SlaConfiguration> SlaConfigurations => Set<SlaConfiguration>();

    public DbSet<CompanyBusinessHours> CompanyBusinessHours => Set<CompanyBusinessHours>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(_dateTimeUtcInterceptor);
    }

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

internal sealed class DateTimeUtcInterceptor : IMaterializationInterceptor
{
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.CreatedAtUtc = EnsureUtc(baseEntity.CreatedAtUtc);
            if (baseEntity.UpdatedAtUtc.HasValue)
                baseEntity.UpdatedAtUtc = EnsureUtc(baseEntity.UpdatedAtUtc.Value);
        }

        if (entity is Ticket ticket)
        {
            ticket.ResponseDeadlineAtUtc = EnsureUtc(ticket.ResponseDeadlineAtUtc);
            if (ticket.StartedWorkAtUtc.HasValue)
                ticket.StartedWorkAtUtc = EnsureUtc(ticket.StartedWorkAtUtc.Value);
            if (ticket.ResolvedAtUtc.HasValue)
                ticket.ResolvedAtUtc = EnsureUtc(ticket.ResolvedAtUtc.Value);
        }

        if (entity is TicketSlaRecord slaRecord)
        {
            slaRecord.ResponseDeadlineAtUtc = EnsureUtc(slaRecord.ResponseDeadlineAtUtc);
            if (slaRecord.BreachedAtUtc.HasValue)
                slaRecord.BreachedAtUtc = EnsureUtc(slaRecord.BreachedAtUtc.Value);
            if (slaRecord.GraceDeadlineUtc.HasValue)
                slaRecord.GraceDeadlineUtc = EnsureUtc(slaRecord.GraceDeadlineUtc.Value);
            if (slaRecord.CanceledAtUtc.HasValue)
                slaRecord.CanceledAtUtc = EnsureUtc(slaRecord.CanceledAtUtc.Value);
            if (slaRecord.ExpiringNotifiedAtUtc.HasValue)
                slaRecord.ExpiringNotifiedAtUtc = EnsureUtc(slaRecord.ExpiringNotifiedAtUtc.Value);
            if (slaRecord.BreachedNotifiedAtUtc.HasValue)
                slaRecord.BreachedNotifiedAtUtc = EnsureUtc(slaRecord.BreachedNotifiedAtUtc.Value);
            if (slaRecord.CanceledNotifiedAtUtc.HasValue)
                slaRecord.CanceledNotifiedAtUtc = EnsureUtc(slaRecord.CanceledNotifiedAtUtc.Value);
        }

        if (entity is RefreshToken refreshToken)
        {
            refreshToken.CreatedAtUtc = EnsureUtc(refreshToken.CreatedAtUtc);
            refreshToken.ExpiresAtUtc = EnsureUtc(refreshToken.ExpiresAtUtc);
            if (refreshToken.RevokedAtUtc.HasValue)
                refreshToken.RevokedAtUtc = EnsureUtc(refreshToken.RevokedAtUtc.Value);
        }

        if (entity is ChatMessage chatMessage)
        {
            chatMessage.SentAtUtc = EnsureUtc(chatMessage.SentAtUtc);
        }

        return entity;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
