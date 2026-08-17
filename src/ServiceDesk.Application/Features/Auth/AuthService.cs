using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using ServiceDesk.Application.Common.Exceptions;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Common.Validation;
using ServiceDesk.Application.Configuration;
using ServiceDesk.Application.DTOs.Auth;
using ServiceDesk.Application.DTOs.Users;
using ServiceDesk.Domain.Identity;
using ValidationException = ServiceDesk.Application.Common.Exceptions.ValidationException;

namespace ServiceDesk.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICompanyRepository _companies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _settings;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly IValidator<AdminCreateUserRequest> _adminCreateUserValidator;
    private readonly IUserRepository _users;

    public AuthService(
        IIdentityService identityService,
        IRefreshTokenRepository refreshTokens,
        ICompanyRepository companies,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator tokenGenerator,
        JwtSettings settings,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<LogoutRequest> logoutValidator,
        IValidator<AdminCreateUserRequest> adminCreateUserValidator,
        IUserRepository users)
    {
        _identityService = identityService;
        _refreshTokens = refreshTokens;
        _companies = companies;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _settings = settings;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _logoutValidator = logoutValidator;
        _adminCreateUserValidator = adminCreateUserValidator;
        _users = users;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_registerValidator, request, cancellationToken);

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
        await ValidationHelper.ValidateAsync(_loginValidator, request, cancellationToken);

        ApplicationUser? user = await _identityService.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !await _identityService.CheckPasswordAsync(user, request.Password))
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
        await ValidationHelper.ValidateAsync(_refreshTokenValidator, request, cancellationToken);

        string presentedTokenHash = HashToken(request.RefreshToken);

        RefreshToken? stored = await _refreshTokens.GetByTokenHashAsync(presentedTokenHash, cancellationToken);

        if (stored is null || !stored.IsActive)
        {
            throw new UnauthorizedException("El refresh token es inválido o ha expirado.");
        }

        ApplicationUser? user = await _identityService.FindByIdAsync(stored.UserId, cancellationToken);

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
        await ValidationHelper.ValidateAsync(_adminCreateUserValidator, request, cancellationToken);

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);

        if (!await _identityService.RoleExistsAsync(request.Role, cancellationToken))
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

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _users.GetAllByCompanyIdAsync(companyId, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_logoutValidator, request, cancellationToken);

        string tokenHash = HashToken(request.RefreshToken);

        RefreshToken? stored = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is not null)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        IReadOnlyList<string> roles = await _identityService.GetRolesAsync(user, cancellationToken);
        string role = roles.SingleOrDefault() ?? Roles.Cliente;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        _refreshTokens.Add(entity);

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
        if (await _identityService.FindByEmailAsync(email, cancellationToken) is not null)
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

        IdentityOperationResult createResult = await _identityService.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            throw new ValidationException(createResult.Errors);
        }

        IdentityOperationResult roleResult = await _identityService.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            throw new ValidationException(roleResult.Errors);
        }

        return user;
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (!await _companies.ExistsAsync(companyId, cancellationToken))
        {
            throw new NotFoundException($"La empresa con id {companyId} no existe.");
        }
    }

    private static string GenerateTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
