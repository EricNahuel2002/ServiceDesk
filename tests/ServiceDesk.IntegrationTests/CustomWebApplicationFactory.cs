using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Domain.Tickets;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.IntegrationTests.Fakes;

namespace ServiceDesk.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DefaultTestConnectionString =
        "Server=.\\SQLEXPRESS;Database=ServiceDesk_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public static string TestConnectionString =>
        Environment.GetEnvironmentVariable("ServiceDesk__TestConnectionString")
        ?? DefaultTestConnectionString;

    public const string SeedCompanyName = "Contoso S.A.";

    public const string AdminEmail = "admin@servicedesk.local";
    public const string AdminPassword = "Admin*123456";
    public const string TechnicianEmail = "tecnico@servicedesk.local";
    public const string TechnicianPassword = "Tecnico*123456";
    public const string ClientEmail = "cliente@servicedesk.local";
    public const string ClientPassword = "Cliente*123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("Jwt:Issuer", "ServiceDesk.Tests");
        builder.UseSetting("Jwt:Audience", "ServiceDesk.Tests.Clients");
        builder.UseSetting("Jwt:SecretKey", "TestSecretKey_AtLeast32Characters_LongEnough_123456");
        builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenExpirationDays", "7");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }

    public async Task<HttpClient> CreateClientForAsync(string email, string password)
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        response.EnsureSuccessStatusCode();

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("La respuesta de login no es válida.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return client;
    }

    public Task<HttpClient> CreateClientForRoleAsync(string role) =>
        role switch
        {
            Roles.Administrador => CreateClientForAsync(AdminEmail, AdminPassword),
            Roles.Tecnico => CreateClientForAsync(TechnicianEmail, TechnicianPassword),
            Roles.Cliente => CreateClientForAsync(ClientEmail, ClientPassword),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Rol no soportado.")
        };

    public async Task RunAsAsync(
        Guid userId,
        Guid companyId,
        string role,
        Func<IServiceScope, Task> action)
    {
        using IServiceScope scope = Services.CreateScope();
        IHttpContextAccessor accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateHttpContext(userId, companyId, role);

        try
        {
            await action(scope);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId, Guid companyId, string role)
    {
        ClaimsIdentity identity = new(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("companyId", companyId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test");

        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    public async Task<Guid> GetCompanyIdAsync(string name)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return await context.Companies
            .Where(c => c.Name == name)
            .Select(c => c.Id)
            .SingleAsync();
    }

    public async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return await context.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync();
    }

    public Task<Guid> GetCategoryIdAsync(Guid companyId, string name) =>
        GetEntityIdAsync(context => context.Categories, c => c.CompanyId == companyId && c.Name == name);

    public Task<Guid> GetPriorityIdAsync(Guid companyId, string name) =>
        GetEntityIdAsync(context => context.Priorities, p => p.CompanyId == companyId && p.Name == name);

    public Task<Guid> GetStatusIdAsync(Guid companyId, string name) =>
        GetEntityIdAsync(context => context.Statuses, s => s.CompanyId == companyId && s.Name == name);

    private async Task<Guid> GetEntityIdAsync<TEntity>(
        Func<ServiceDeskDbContext, IQueryable<TEntity>> selector,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return await selector(context)
            .Where(predicate)
            .Select(entity => entity.Id)
            .SingleAsync(cancellationToken);
    }

    public async Task<DateTime?> GetResolvedAtUtcAsync(Guid ticketId)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return await context.Tickets
            .Where(t => t.Id == ticketId)
            .Select(t => t.ResolvedAtUtc)
            .SingleAsync();
    }

    public async Task<int> CountTicketCommentsAsync(Guid ticketId)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        return await context.TicketComments
            .Where(c => c.TicketId == ticketId)
            .CountAsync();
    }

    public async Task ResetTicketsAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();
        context.Tickets.RemoveRange(context.Tickets);
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateCompanyAsync(string name)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();

        string uniqueName = $"{name} {Guid.NewGuid():N}";
        Company company = new() { Id = Guid.NewGuid(), Name = uniqueName, IsActive = true };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        return company.Id;
    }

    public async Task CreateCatalogAsync(Guid companyId)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();

        context.Categories.Add(new Category { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Hardware", IsActive = true });
        context.Priorities.Add(new Priority { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Media", SortOrder = 2, IsActive = true });
        context.Statuses.Add(new Status { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Nuevo", SortOrder = 1, IsActive = true });
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        Guid companyId,
        string role,
        bool isActive = true)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();

        Guid roleId = await context.Roles
            .Where(r => r.Name == role)
            .Select(r => r.Id)
            .SingleAsync();

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId,
            IsActive = isActive,
            EmailConfirmed = true
        };

        context.Users.Add(user);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
        await context.SaveChangesAsync();

        return user.Id;
    }

    public async Task<Guid> CreateUserWithPasswordAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid companyId,
        string role)
    {
        using IServiceScope scope = Services.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId,
            IsActive = true,
            EmailConfirmed = true
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        EnsureSuccess(createResult);

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, role);
        EnsureSuccess(roleResult);

        return user.Id;
    }

    public async Task<Guid> CreateTicketAsync(
        Guid companyId,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        Guid createdById,
        string title,
        string description,
        Guid? assignedToId = null,
        DateTime? createdAtUtc = null)
    {
        using IServiceScope scope = Services.CreateScope();
        ServiceDeskDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDeskDbContext>();

        Ticket ticket = new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CategoryId = categoryId,
            PriorityId = priorityId,
            StatusId = statusId,
            CreatedById = createdById,
            AssignedToId = assignedToId,
            Title = title,
            Description = description
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        if (createdAtUtc is not null)
        {
            ticket.CreatedAtUtc = createdAtUtc.Value;
            await context.SaveChangesAsync();
        }

        return ticket.Id;
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private sealed record LoginResponse(string AccessToken);
}
