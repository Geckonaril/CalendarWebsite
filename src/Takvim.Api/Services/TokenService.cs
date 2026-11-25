using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Takvim.Api.Models;

namespace Takvim.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Takvim.Api";

    public string Audience { get; set; } = "Takvim.Web";

    /// <summary>HS256 imzalama anahtarı (en az 32 karakter).</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 480;

    public SymmetricSecurityKey GetSigningKey() =>
        new(Encoding.UTF8.GetBytes(Key));
}

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken Create(AppUser user, IEnumerable<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                _options.GetSigningKey(),
                SecurityAlgorithms.HmacSha256));

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires);
    }
}
