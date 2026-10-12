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
using Talabat.APIs.Errors;
using Talabat.APIs.Helpers;
using Talabat.Core.Models.Identity;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Talabat.Tests;

public class AccountFlowTests
{
    [Fact]
    public async Task PostAddressCreatesGeneratedIdAndGetAddressReturnsTheSavedAddress()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var identityDb = new TestIdentityDbContext(options);
        var user = new AppUser
        {
            Id = "address-user-1",
            UserName = "ada@example.com",
            Email = "ada@example.com",
            DisplayName = "Ada"
        };
        identityDb.Users.Add(user);
        await identityDb.SaveChangesAsync();

        var userManager = CreateUserManager(identityDb, user);
        userManager.Setup(manager => manager.Users).Returns(identityDb.Users);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.UpdateAsync(user)).Returns(async () =>
        {
            await identityDb.SaveChangesAsync();
            return IdentityResult.Success;
        });

        var mapperConfiguration = new MapperConfiguration(configuration => configuration.AddProfile<MappingProfiles>());
        var controller = CreateAddressController(userManager.Object, identityDb, mapperConfiguration.CreateMapper(), user.Id);

        var postResult = await controller.CreateUserAddress(new AddressInputDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Street = "1 Main St",
            City = "London",
            Country = "UK"
        });

        var createdResponse = Assert.IsType<CreatedAtActionResult>(postResult.Result);
        var createdAddress = Assert.IsType<AddressDto>(createdResponse.Value);
        Assert.True(createdAddress.Id > 0);

        var getResult = await controller.GetUserAddress();

        var savedAddress = Assert.IsType<AddressDto>(Assert.IsType<OkObjectResult>(getResult.Result).Value);
        Assert.Equal(createdAddress.Id, savedAddress.Id);
        Assert.Equal("Ada", savedAddress.FirstName);
        Assert.Equal("Lovelace", savedAddress.LastName);
        Assert.Equal("1 Main St", savedAddress.Street);
        Assert.Equal("London", savedAddress.City);
        Assert.Equal("UK", savedAddress.Country);
    }

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

    [Fact]
    public async Task UpdatePasswordRejectsReusingCurrentPassword()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var identityDb = new TestIdentityDbContext(options);
        var user = new AppUser { Id = "customer-1", Email = "customer@example.com", SecurityStamp = "stamp-1" };
        var userManager = CreateUserManager(identityDb, user);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "CurrentPass123!")).ReturnsAsync(true);
        var controller = CreatePasswordController(userManager.Object, identityDb, user.Id);

        var result = await controller.UpdateUserPassword(new UpdatePasswordDto
        {
            CurrentPassword = "CurrentPass123!",
            NewPassword = "CurrentPass123!"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("different", Assert.Single(Assert.IsType<ApiValidationErrorResponse>(badRequest.Value).Errors));
        userManager.Verify(manager => manager.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePasswordRejectsIncorrectCurrentPassword()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var identityDb = new TestIdentityDbContext(options);
        var user = new AppUser { Id = "customer-3", Email = "customer@example.com" };
        var userManager = CreateUserManager(identityDb, user);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "WrongPass123!")).ReturnsAsync(false);
        var controller = CreatePasswordController(userManager.Object, identityDb, user.Id);

        var result = await controller.UpdateUserPassword(new UpdatePasswordDto
        {
            CurrentPassword = "WrongPass123!",
            NewPassword = "NewPass456!"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("incorrect", Assert.Single(Assert.IsType<ApiValidationErrorResponse>(badRequest.Value).Errors));
        userManager.Verify(manager => manager.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePasswordReturnsFreshTokensAfterChangingPassword()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var identityDb = new TestIdentityDbContext(options);
        var user = new AppUser
        {
            Id = "customer-2",
            Email = "customer@example.com",
            DisplayName = "Customer",
            SecurityStamp = "old-stamp"
        };
        identityDb.Users.Add(user);
        var oldRefreshToken = new UserRefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "old-refresh-hash",
            FamilyId = "old-refresh-family",
            UserSecurityStamp = "old-stamp",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };
        identityDb.UserRefreshTokens.Add(oldRefreshToken);
        await identityDb.SaveChangesAsync();

        var userManager = CreateUserManager(identityDb, user);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "OldPass123!")).ReturnsAsync(true);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "NewPass456!")).ReturnsAsync(false);
        userManager.Setup(manager => manager.ChangePasswordAsync(user, "OldPass123!", "NewPass456!"))
            .Callback(() => user.SecurityStamp = "new-stamp")
            .ReturnsAsync(IdentityResult.Success);

        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.CreateTokenAsync(user, userManager.Object)).ReturnsAsync("fresh-access-token");
        authService.Setup(service => service.CreateRefreshToken()).Returns("fresh-refresh-token");
        authService.Setup(service => service.HashRefreshToken("fresh-refresh-token")).Returns("fresh-refresh-hash");
        var controller = CreatePasswordController(userManager.Object, identityDb, user.Id, authService.Object);

        var result = await controller.UpdateUserPassword(new UpdatePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!"
        });

        var response = Assert.IsType<UserDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("fresh-access-token", response.Token);
        Assert.Equal("fresh-refresh-token", response.RefreshToken);
        var refreshTokens = await identityDb.UserRefreshTokens.ToListAsync();
        Assert.Equal(2, refreshTokens.Count);
        Assert.NotNull(oldRefreshToken.RevokedAtUtc);
        var storedRefreshToken = refreshTokens.Single(token => token.TokenHash == "fresh-refresh-hash");
        Assert.Equal("new-stamp", storedRefreshToken.UserSecurityStamp);
        Assert.Equal("fresh-refresh-hash", storedRefreshToken.TokenHash);
    }

    private static AccountController CreatePasswordController(
        UserManager<AppUser> userManager,
        AppIdentityDbContext identityDb,
        string userId,
        IAuthService? authService = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:RefreshTokenLifeTimeDays"] = "30"
        }).Build();
        var controller = new AccountController(
            userManager,
            CreateSignInManager(userManager).Object,
            Mock.Of<IMapper>(),
            authService ?? Mock.Of<IAuthService>(),
            config,
            identityDb)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId)], "test"))
                }
            }
        };
        return controller;
    }

    private static AccountController CreateAddressController(
        UserManager<AppUser> userManager,
        AppIdentityDbContext identityDb,
        IMapper mapper,
        string userId)
    {
        var controller = new AccountController(
            userManager,
            CreateSignInManager(userManager).Object,
            mapper,
            Mock.Of<IAuthService>(),
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            identityDb)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId)], "test"))
                }
            }
        };
        return controller;
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
