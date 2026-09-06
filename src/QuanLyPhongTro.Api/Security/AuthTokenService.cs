using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuanLyPhongTro.Application.Options;
using QuanLyPhongTro.Application.Security;

namespace QuanLyPhongTro.Api.Security;

/// <summary>Phát hành và xác thực JWT access/refresh token.</summary>
public class AuthTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;

    public AuthTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    }

    public AccessTokenResult CreateAccessToken(int userId, string username, IList<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("username", username)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = WriteToken(claims, expiresAt, "access");
        return new AccessTokenResult(token, expiresAt);
    }

    public string CreateRefreshToken(int userId)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("token_type", "refresh")
        };
        return WriteToken(claims, expiresAt, "refresh");
    }

    public bool TryGetUserIdFromRefreshToken(string refreshToken, out int userId)
    {
        userId = 0;
        try
        {
            var principal = ValidateToken(refreshToken);
            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (principal.FindFirst("token_type")?.Value != "refresh" || idClaim is null)
                return false;
            return int.TryParse(idClaim.Value, out userId);
        }
        catch
        {
            return false;
        }
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expiresAt, string audience)
    {
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = audience == "access" ? _options.Audience : _options.Issuer,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        // Quan trọng: không để handler rút gọn claim (role→"role", nameid→"nameid"),
        // giữ nguyên dạng đầy đủ ClaimTypes để khớp với phía đọc (MapInboundClaims=false).
        var handler = new JwtSecurityTokenHandler { OutboundClaimTypeMap = new Dictionary<string, string>() };
        return handler.CreateEncodedJwt(descriptor);
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Issuer, // refresh token dùng audience = issuer
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        return handler.ValidateToken(token, parameters, out _);
    }
}
