using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Domain.Entities;

namespace RoleBaseAuth.Infrastructure.Identity;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    TokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
}

public class TokenResult
{
    public string Token { get; set; }
    public string JwtId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class AuthService(IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public TokenResult GenerateAccessToken(User user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        // Add roles
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));

            // Add permissions
            foreach (var permission in userRole.Role.RolePermissions)
            {
                claims.Add(new Claim("Permission", permission.Permission.Name));
            }
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"]));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new TokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            JwtId = jwtId,
            ExpiresAt = expiresAt
        };
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token validation failed");
            return null;
        }
    }
}