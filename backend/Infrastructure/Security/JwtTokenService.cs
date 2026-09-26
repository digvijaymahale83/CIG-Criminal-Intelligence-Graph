using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security;

public class JwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "CriminalNetworkAnalysisPlatformSecretKey2026SecureDevKey!";
        _issuer = configuration["Jwt:Issuer"] ?? "CriminalNetworkAnalysis";
        _audience = configuration["Jwt:Audience"] ?? "CriminalNetworkAnalysisClient";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 480; // 8 hours default
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        if (key.Length < 32)
        {
            // Pad or expand key if needed to meet HMAC-SHA256 minimum key size of 256 bits (32 bytes)
            var expandedKey = new byte[32];
            Encoding.UTF8.GetBytes(_secretKey).CopyTo(expandedKey, 0);
            key = expandedKey;
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("badge_number", user.BadgeNumber),
            new("agency", user.Agency),
            new("rank", user.Rank),
            new("unit", user.Unit)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);
        if (key.Length < 32)
        {
            var expandedKey = new byte[32];
            Encoding.UTF8.GetBytes(_secretKey).CopyTo(expandedKey, 0);
            key = expandedKey;
        }

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
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
