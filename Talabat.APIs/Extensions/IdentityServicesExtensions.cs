using System.Text;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Talabat.Core.Models.Identity;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using Talabat.Services;

namespace Talabat.APIs.Extensions;

public static class IdentityServicesExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
    {
        var secretKey = config["JWT:SecretKey"];
        var validIssuer = config["JWT:ValidIssuer"];
        var validAudience = config["JWT:ValidAudience"];
        var tokenVersion = config["JWT:TokenVersion"];
        var accessTokenLifeTimeMinutes = config["JWT:AccessTokenLifeTimeMinutes"];
        var refreshTokenLifeTimeDays = config["JWT:RefreshTokenLifeTimeDays"];

        if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT:SecretKey is required and must be at least 32 bytes. Configure the JWT__SecretKey environment variable.");
        }

        if (string.IsNullOrWhiteSpace(validIssuer) || string.IsNullOrWhiteSpace(validAudience))
        {
            throw new InvalidOperationException(
                "JWT:ValidIssuer and JWT:ValidAudience are required. Configure JWT__ValidIssuer and JWT__ValidAudience.");
        }

        if (string.IsNullOrWhiteSpace(tokenVersion))
        {
            throw new InvalidOperationException("JWT:TokenVersion is required to invalidate previously issued access tokens.");
        }

        if (!double.TryParse(accessTokenLifeTimeMinutes, NumberStyles.Float, CultureInfo.InvariantCulture, out var accessLifetimeMinutes) ||
            accessLifetimeMinutes is <= 0 or > 15)
        {
            throw new InvalidOperationException(
                "JWT:AccessTokenLifeTimeMinutes must be greater than 0 and at most 15. Configure JWT__AccessTokenLifeTimeMinutes.");
        }

        if (!double.TryParse(refreshTokenLifeTimeDays, NumberStyles.Float, CultureInfo.InvariantCulture, out var refreshLifetimeDays) ||
            refreshLifetimeDays is <= 0 or > 90)
        {
            throw new InvalidOperationException(
                "JWT:RefreshTokenLifeTimeDays must be greater than 0 and at most 90. Configure JWT__RefreshTokenLifeTimeDays.");
        }

        services.AddScoped(typeof(IAuthService), typeof(AuthService));
        services.AddIdentity<AppUser,IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<AppIdentityDbContext>();
        services.AddAuthentication( options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer( options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = validAudience,
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal?.FindFirst("token_version")?.Value != tokenVersion)
                        {
                            context.Fail("The access token version is no longer accepted.");
                            return;
                        }

                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenSecurityStamp = context.Principal?.FindFirst("security_stamp")?.Value;
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                        var user = userId is null ? null : await userManager.FindByIdAsync(userId);
                        if (user is null || tokenSecurityStamp is null ||
                            !CryptographicOperations.FixedTimeEquals(
                                Encoding.UTF8.GetBytes(tokenSecurityStamp),
                                Encoding.UTF8.GetBytes(user.SecurityStamp ?? string.Empty)))
                        {
                            context.Fail("The account session has been revoked.");
                        }
                    }
                };
            });
        return services;
    }
} 
