using backend.Models.Entities;

namespace backend.Utilities.Interface;

public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, IReadOnlyCollection<string> roles);
}
