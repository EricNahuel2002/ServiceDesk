using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Infrastructure.Configuration;
using ServiceDesk.Infrastructure.Persistence;
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

        services.AddSingleton<IOptions<JwtSettings>>(Options.Create(jwtSettings));

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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ServiceDeskDbInitializer>();

        return services;
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
