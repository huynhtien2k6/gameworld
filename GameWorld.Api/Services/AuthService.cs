using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameWorld.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace GameWorld.Api.Services;

public sealed class AuthService(IConfiguration configuration)
{
    public int AccessTokenMinutes => Math.Clamp(configuration.GetValue("JwtSettings:ExpiryMinutes", 60), 5, 60);

    public string CreateToken(Account account, AccountSession session) => CreateJwt([
        new Claim(ClaimTypes.Name, account.Username),
        new Claim(ClaimTypes.Role, "Player"),
        new Claim("accountId", account.Id.ToString()),
        new Claim("sessionId", session.Id.ToString())
    ]);

    public string CreateAdminToken(AdminUser admin) => CreateJwt([
        new Claim(ClaimTypes.Name, admin.Username),
        new Claim(ClaimTypes.Role, "Admin"),
        new Claim("adminId", admin.Id.ToString())
    ]);

    public static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public static string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string CreateJwt(IEnumerable<Claim> claims)
    {
        var key = configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT signing key is missing.");
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims.Append(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))),
            notBefore: now,
            expires: now.AddMinutes(AccessTokenMinutes),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
