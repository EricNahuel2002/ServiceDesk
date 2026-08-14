using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Configuration;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;
using ServiceDesk.Infrastructure.Persistence.Seed;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ServiceDeskDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ServiceDeskDbContext>();

        services.AddScoped<IPasswordHasher<ApplicationUser>, BcryptPasswordHasher>();

        JwtSettings jwtSettings = ReadJwtSettings(configuration);

        services.AddSingleton(jwtSettings);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.RequireAdministrador, policy => policy.RequireRole(Roles.Administrador))
            .AddPolicy(AuthPolicies.RequireTecnico, policy => policy.RequireRole(Roles.Tecnico, Roles.Administrador))
            .AddPolicy(AuthPolicies.RequireCliente, policy => policy.RequireRole(Roles.Cliente));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ServiceDeskDbContext>());
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ServiceDeskDbInitializer>();

        EmailSettings emailSettings = ReadEmailSettings(configuration);
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettingsSection));
        services.AddScoped<IEmailService, EmailService>();

        services.Configure<BlobStorageSettings>(configuration.GetSection(BlobStorageSettingsSection));
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        QueueStorageSettings queueStorageSettings = ReadQueueStorageSettings(configuration);
        services.Configure<QueueStorageSettings>(configuration.GetSection(QueueStorageSettingsSection));
        services.AddScoped<IQueueStorageService, QueueStorageService>();

        return services;
    }

    private const string EmailSettingsSection = "Email";

    private const string BlobStorageSettingsSection = "BlobStorage";

    private const string QueueStorageSettingsSection = "QueueStorage";

    private static EmailSettings ReadEmailSettings(IConfiguration configuration)
    {
        EmailSettings settings = configuration.GetSection(EmailSettingsSection).Get<EmailSettings>() ?? new EmailSettings();

        if (settings.Enabled
            && (string.IsNullOrWhiteSpace(settings.Host)
                || string.IsNullOrWhiteSpace(settings.FromEmail)))
        {
            throw new InvalidOperationException(
                "La sección Email está habilitada pero faltan valores obligatorios (Host, FromEmail).");
        }

        return settings;
    }

    private static QueueStorageSettings ReadQueueStorageSettings(IConfiguration configuration)
    {
        QueueStorageSettings settings =
            configuration.GetSection(QueueStorageSettingsSection).Get<QueueStorageSettings>() ?? new QueueStorageSettings();

        if (settings.Enabled && string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "La sección QueueStorage está habilitada pero falta ConnectionString.");
        }

        return settings;
    }

    private static JwtSettings ReadJwtSettings(IConfiguration configuration)
    {
        JwtSettings settings = new()
        {
            Issuer = configuration["Jwt:Issuer"] ?? string.Empty,
            Audience = configuration["Jwt:Audience"] ?? string.Empty,
            SecretKey = configuration["Jwt:SecretKey"] ?? string.Empty,
            AccessTokenExpirationMinutes = ParseInt(configuration["Jwt:AccessTokenExpirationMinutes"], 15),
            RefreshTokenExpirationDays = ParseInt(configuration["Jwt:RefreshTokenExpirationDays"], 7)
        };

        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey es obligatoria y debe tener al menos 32 caracteres.");
        }

        return settings;
    }

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out int parsed) ? parsed : defaultValue;
}
