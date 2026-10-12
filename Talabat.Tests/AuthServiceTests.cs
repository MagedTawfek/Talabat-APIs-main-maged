using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Talabat.Core.Models.Identity;
using Talabat.Services;

namespace Talabat.Tests;

public class AuthServiceTests
{
    private const string Secret = "a-test-secret-that-is-at-least-32-characters-long";

    [Fact]
    public async Task CreateToken_UsesConfiguredIssuerAudienceAndCurrentUserRoles()
    {
        var user = new AppUser { Id = "user-1", UserName = "ada", Email = "ada@example.com" };
        var userManager = CreateUserManager(user);
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.FindByNameAsync("Customer")).ReturnsAsync(new IdentityRole("Customer"));
        roleManager.Setup(manager => manager.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([]);
        var service = new AuthService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:SecretKey"] = Secret,
            ["JWT:ValidIssuer"] = "talabat-tests",
            ["JWT:ValidAudience"] = "talabat-client",
            ["JWT:TokenVersion"] = "2",
            ["JWT:AccessTokenLifeTimeMinutes"] = "10"
        }).Build(), roleManager.Object);

        var tokenString = await service.CreateTokenAsync(user, userManager.Object);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        Assert.Equal("talabat-tests", token.Issuer);
        Assert.Contains("talabat-client", token.Audiences);
        Assert.Equal("ada@example.com", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.True(DateTime.UtcNow.AddMinutes(9) < token.ValidTo);
        Assert.True(DateTime.UtcNow.AddMinutes(11) > token.ValidTo);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Customer");
        Assert.Equal("2", token.Claims.Single(claim => claim.Type == "token_version").Value);
        Assert.Equal(user.SecurityStamp, token.Claims.Single(claim => claim.Type == "security_stamp").Value);

        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "talabat-tests",
            ValidateAudience = true,
            ValidAudience = "talabat-client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ValidateLifetime = true
        };
        var principal = new JwtSecurityTokenHandler().ValidateToken(tokenString, validation, out _);
        Assert.Equal("user-1", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("ada@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal("Customer", principal.FindFirstValue(ClaimTypes.Role));
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Driver")]
    [InlineData("Admin")]
    public async Task CreateTokenPreservesEachSupportedAccountRole(string roleName)
    {
        var user = new AppUser { Id = $"{roleName.ToLowerInvariant()}-1", UserName = "user", Email = "user@example.com" };
        var userManager = CreateUserManager(user, roleName);
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.FindByNameAsync(roleName)).ReturnsAsync(new IdentityRole(roleName));
        roleManager.Setup(manager => manager.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([]);
        var service = new AuthService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:SecretKey"] = Secret,
            ["JWT:ValidIssuer"] = "talabat-tests",
            ["JWT:ValidAudience"] = "talabat-client",
            ["JWT:TokenVersion"] = "2",
            ["JWT:AccessTokenLifeTimeMinutes"] = "10"
        }).Build(), roleManager.Object);

        var token = await service.CreateTokenAsync(user, userManager.Object);
        var principal = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(principal.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == roleName);
    }

    [Fact]
    public void CreateRefreshToken_ReturnsUniqueCryptographicallyRandomValues()
    {
        var service = new AuthService(new ConfigurationBuilder().Build(), CreateRoleManager().Object);

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(88, first.Length);
        Assert.Equal(88, second.Length);
        Assert.Equal(64, service.HashRefreshToken(first).Length);
        Assert.NotEqual(first, service.HashRefreshToken(first));
    }

    private static Mock<UserManager<AppUser>> CreateUserManager(AppUser user, string role = "Customer")
    {
        var store = new Mock<IUserStore<AppUser>>();
        var manager = new Mock<UserManager<AppUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<AppUser>>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<AppUser>>>());
        manager.Setup(userManager => userManager.GetRolesAsync(user)).ReturnsAsync([role]);
        return manager;
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());
    }
}
