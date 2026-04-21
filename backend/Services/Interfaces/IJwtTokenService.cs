using backend.Models.Entities;

namespace backend.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, IReadOnlyCollection<string> roles);
}
