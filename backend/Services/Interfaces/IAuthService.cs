using backend.Models.Dtos;

namespace backend.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
}
