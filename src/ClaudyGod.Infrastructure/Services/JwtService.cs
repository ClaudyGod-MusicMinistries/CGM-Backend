using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClaudyGod.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public JwtService(IConfiguration config)
    {
        _config = config;
        _key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        _issuer = config["Jwt:Issuer"] ?? "ClaudyGod.API";
        _audience = config["Jwt:Audience"] ?? "ClaudyGod.Client";
        _accessTokenExpiryMinutes = int.TryParse(config["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 60;
        _refreshTokenExpiryDays = int.TryParse(config["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string? ipAddress)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
