using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;
using Talabat.APIs.Helpers;
using Talabat.Core.Models.Identity;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Talabat.Tests;

public class AccountFlowTests
{
    [Fact]
    public async Task CustomerCanRegisterLoginRefreshAndReadGeneratedAccountId()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var identityDb = new TestIdentityDbContext(options);
        var user = new AppUser();
        var userManager = CreateUserManager(identityDb, user);
        userManager.Setup(manager => manager.FindByEmailAsync("ada@example.com")).ReturnsAsync(() =>
            identityDb.Users.SingleOrDefault(account => account.Email == "ada@example.com"));
        userManager.Setup(manager => manager.FindByIdAsync("customer-test-1")).ReturnsAsync(() =>
            identityDb.Users.SingleOrDefault(account => account.Id == "customer-test-1"));
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<AppUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.CheckPasswordSignInAsync(
                It.IsAny<AppUser>(), "StrongPass123!", true))
            .ReturnsAsync(IdentitySignInResult.Success);

        var tokenNumber = 0;
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.CreateTokenAsync(It.IsAny<AppUser>(), userManager.Object))
            .ReturnsAsync("test-access-token");
        authService.Setup(service => service.CreateRefreshToken())
            .Returns(() => $"refresh-{Interlocked.Increment(ref tokenNumber)}");
        authService.Setup(service => service.HashRefreshToken(It.IsAny<string>()))
            .Returns<string>(token => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:RefreshTokenLifeTimeDays"] = "30"
        }).Build();
        var controller = new AccountController(userManager.Object, signInManager.Object, Mock.Of<IMapper>(),
            authService.Object, configuration, identityDb);

        var registration = await controller.Register(new RegisterDto
        {
            Email = "ada@example.com",
            DisplayName = "Ada",
            PhoneNumber = "+44123456789",
            Password = "StrongPass123!"
        });
        var registered = Assert.IsType<UserDto>(registration.Value);
        Assert.Equal("customer-test-1", registered.Id);
        Assert.Equal("Customer", Assert.Single(await userManager.Object.GetRolesAsync(user)));

        var login = await controller.Login(new LoginDto { Email = "ada@example.com", Password = "StrongPass123!" });
        var signedIn = Assert.IsType<UserDto>(login.Value);
        Assert.Equal(registered.Id, signedIn.Id);

        var refreshed = await controller.RefreshToken(new RefreshTokenDto { RefreshToken = signedIn.RefreshToken });
        var nextSession = Assert.IsType<UserDto>(refreshed.Value);
        Assert.Equal(registered.Id, nextSession.Id);
        Assert.NotEqual(signedIn.RefreshToken, nextSession.RefreshToken);
        Assert.All(await identityDb.UserRefreshTokens.ToListAsync(), token => Assert.Equal(registered.Id, token.UserId));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, registered.Id)], "test"))
            }
        };
        var profile = await controller.GetCurrentUser();
        var profileDto = Assert.IsType<UserProfileDto>(Assert.IsType<OkObjectResult>(profile.Result).Value);
        Assert.Equal(registered.Id, profileDto.Id);
    }

    private static Mock<UserManager<AppUser>> CreateUserManager(AppIdentityDbContext identityDb, AppUser user)
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
            Mock.Of<ILogger<UserManager<AppUser>>>())
        {
            CallBase = true
        };
        manager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), "StrongPass123!"))
            .Callback<AppUser, string>((created, _) =>
            {
                created.Id = "customer-test-1";
                created.SecurityStamp = Guid.NewGuid().ToString("N");
                identityDb.Users.Add(created);
                identityDb.SaveChanges();
                user.Id = created.Id;
                user.SecurityStamp = created.SecurityStamp;
            })
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        manager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        manager.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(["Customer"]);
        return manager;
    }

    private sealed class TestIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : AppIdentityDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserRefreshToken>()
                .Property(token => token.RowVersion)
                .IsRequired(false)
                .ValueGeneratedNever();
        }
    }

    private static Mock<SignInManager<AppUser>> CreateSignInManager(UserManager<AppUser> userManager)
    {
        var manager = new Mock<SignInManager<AppUser>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<AppUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<AppUser>>());
        manager.Setup(signIn => signIn.CheckPasswordSignInAsync(
                It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(IdentitySignInResult.Success);
        return manager;
    }
}
