using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Auth;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Infrastructure.Configuration;
using ServiceDesk.Infrastructure.Persistence;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ServiceDeskDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _settings;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly IValidator<AdminCreateUserRequest> _adminCreateUserValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ServiceDeskDbContext context,
        IJwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> settings,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<LogoutRequest> logoutValidator,
        IValidator<AdminCreateUserRequest> adminCreateUserValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenGenerator = tokenGenerator;
        _settings = settings.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _logoutValidator = logoutValidator;
        _adminCreateUserValidator = adminCreateUserValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);

        ApplicationUser user = await CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.CompanyId,
            Roles.Cliente,
            cancellationToken);

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Las credenciales son inválidas.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("El usuario está desactivado.");
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_refreshTokenValidator, request, cancellationToken);

        string presentedTokenHash = HashToken(request.RefreshToken);

        RefreshToken? stored = await _context.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == presentedTokenHash, cancellationToken);

        if (stored is null || !stored.IsActive)
        {
            throw new UnauthorizedException("El refresh token es inválido o ha expirado.");
        }

        ApplicationUser? user = await _userManager.FindByIdAsync(stored.UserId.ToString());

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("El usuario asociado al refresh token no es válido.");
        }

        (string rawRefreshToken, RefreshToken newToken) = CreateRefreshToken(user);

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = newToken.TokenHash;

        return await BuildAuthResponseAsync(user, rawRefreshToken, newToken, cancellationToken);
    }

    public async Task<AuthResponse> CreateUserAsync(AdminCreateUserRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_adminCreateUserValidator, request, cancellationToken);

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);

        if (await _roleManager.FindByNameAsync(request.Role) is null)
        {
            throw new NotFoundException($"El rol {request.Role} no existe.");
        }

        ApplicationUser user = await CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.CompanyId,
            request.Role,
            cancellationToken);

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_logoutValidator, request, cancellationToken);

        string tokenHash = HashToken(request.RefreshToken);

        RefreshToken? stored = await _context.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (stored is not null)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResponse> IssueTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        (string rawRefreshToken, RefreshToken refreshToken) = CreateRefreshToken(user);

        return await BuildAuthResponseAsync(user, rawRefreshToken, refreshToken, cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user,
        string rawRefreshToken,
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        string role = (await _userManager.GetRolesAsync(user)).SingleOrDefault() ?? Roles.Cliente;

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = _tokenGenerator.GenerateAccessToken(user, role),
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = new AuthUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                CompanyId = user.CompanyId,
                Role = role
            }
        };
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(ApplicationUser user)
    {
        string rawToken = GenerateTokenValue();

        RefreshToken entity = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays)
        };

        _context.RefreshTokens.Add(entity);

        return (rawToken, entity);
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid companyId,
        string role,
        CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Email"] = ["Ya existe un usuario con ese email."]
            });
        }

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId,
            EmailConfirmed = true,
            IsActive = true
        };

        IdentityResult createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            throw new ValidationException(ToErrorDictionary(createResult));
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            throw new ValidationException(ToErrorDictionary(roleResult));
        }

        return user;
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        bool exists = await _context.Companies.AnyAsync(c => c.Id == companyId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"La empresa con id {companyId} no existe.");
        }
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            IReadOnlyDictionary<string, string[]> errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }
    }

    private static IReadOnlyDictionary<string, string[]> ToErrorDictionary(IdentityResult result) =>
        result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(group => group.Key, group => group.Select(e => e.Description).ToArray());

    private static string GenerateTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
