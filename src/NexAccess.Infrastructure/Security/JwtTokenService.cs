using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NexAccess.Application.Interfaces;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    public const string RoleClaim = "role";
    public const string NameClaim = "name";

    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));

    public IssuedToken Create(User user)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(NameClaim, user.Username),
                new Claim(RoleClaim, SystemRoles.From(user.SystemRole)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            }),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options.Key), SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
