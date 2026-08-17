using ServiceDesk.Application.DTOs.Auth;
using ServiceDesk.Application.DTOs.Users;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> CreateUserAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}
