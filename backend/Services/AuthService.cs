using backend.Models.Dtos;
using backend.Models.Entities;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace backend.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IJwtTokenService jwt,
        IPasswordHasher<User> passwordHasher)
    {
        _users = users;
        _roles = roles;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            // Persist rehash via context - skip for demo brevity or add user update repo method
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (token, expires) = _jwt.CreateToken(user, roles);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            User = MapUser(user, roles),
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
        {
            return null;
        }

        var buyerRole = await _roles.GetByNameAsync("Buyer", cancellationToken);
        if (buyerRole is null)
        {
            throw new InvalidOperationException("Buyer role is not seeded.");
        }

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.UserRoles.Add(new UserRole { RoleId = buyerRole.Id });

        await _users.AddAsync(user, cancellationToken);

        var roles = new[] { buyerRole.Name };
        var (token, expires) = _jwt.CreateToken(user, roles);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            User = MapUser(user, roles.ToList()),
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return MapUser(user, roles);
    }

    private static UserDto MapUser(User user, IReadOnlyList<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles,
        };
    }
}
