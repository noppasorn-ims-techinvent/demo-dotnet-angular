using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models.Entities;
using backend.Options;
using backend.Utilities.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Utilities;

public sealed class JwtService : IJwtService
{
    private readonly AppSettings _app;
    private readonly JwtOptions _options;

    public JwtService(AppSettings app, IOptions<JwtOptions> options)
    {
        _app = app;
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_app.Jwt.Secret))
        {
            throw new InvalidOperationException("AppSettings:Jwt:Secret is not configured.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expires = DateTime.UtcNow.AddSeconds(_app.Jwt.ExpireInSec);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_app.Jwt.Secret));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return (handler.WriteToken(token), expires);
    }
}
