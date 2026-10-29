
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
    public const string ChannelClaim = "channel";
    public const string SessionClaim = "sid";

    public const string WebChannel = "web";
    public const string MobileChannel = "mobile";

    private readonly JwtOptions _options;
    private readonly IClock _clock;

    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(
        IOptions<JwtOptions> options,
        IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public static SymmetricSecurityKey CreateSigningKey(string key)
        => new(Encoding.UTF8.GetBytes(key));

    // Token para el panel administrativo.
    public IssuedToken Create(User user)
    {
        return GenerateToken(
            user,
            SystemRoles.From(user.SystemRole),
            WebChannel,
            null);
    }

    // Token para la app Android.
    public IssuedToken CreateMobile(
        User user,
        Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "La sesión móvil es obligatoria.",
                nameof(sessionId));
        }

        return GenerateToken(
            user,
            SystemRoles.Empleado,
            MobileChannel,
            sessionId);
    }

    private IssuedToken GenerateToken(
        User user,
        string effectiveRole,
        string channel,
        Guid? sessionId)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(
            _options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(NameClaim, user.Username),
            new(RoleClaim, effectiveRole),
            new(ChannelClaim, channel),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        };

        if (sessionId.HasValue)
        {
            claims.Add(new Claim(
                SessionClaim,
                sessionId.Value.ToString("D")));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options.Key),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(
            _handler.CreateToken(descriptor),
            expiresAt);
    }
}
