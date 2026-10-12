using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Talabat.Core.Models.Identity;
using Talabat.Core.Services.Contract;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Talabat.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthService(IConfiguration config, RoleManager<IdentityRole> roleManager)
    {
        _config = config;
        _roleManager = roleManager;
    }
    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    public async Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager)
    {
        var secretKey = _config["JWT:SecretKey"]
            ?? throw new InvalidOperationException("JWT:SecretKey is not configured.");
        var issuer = _config["JWT:ValidIssuer"]
            ?? throw new InvalidOperationException("JWT:ValidIssuer is not configured.");
        var audience = _config["JWT:ValidAudience"]
            ?? throw new InvalidOperationException("JWT:ValidAudience is not configured.");
        var tokenVersion = _config["JWT:TokenVersion"]
            ?? throw new InvalidOperationException("JWT:TokenVersion is not configured.");
        if (!double.TryParse(_config["JWT:AccessTokenLifeTimeMinutes"], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lifetimeMinutes) || lifetimeMinutes is <= 0 or > 15)
            throw new InvalidOperationException("JWT:AccessTokenLifeTimeMinutes must be greater than 0 and at most 15.");

        var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.GivenName, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("token_version", tokenVersion),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
        };
        var userRoles = await userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
            var identityRole = await _roleManager.FindByNameAsync(role);
            if (identityRole is not null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(identityRole);
                authClaims.AddRange(roleClaims.Where(claim => claim.Type == RolePermission.ClaimType));
            }
        }

        var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
