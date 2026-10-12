using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Talabat.Core.Models.Identity;

namespace Talabat.Repository.Identity;

public static class AppIdentityDbContextSeed
{
    private static readonly string[] DefaultRoles = ["Admin", "Customer", "Driver"];
    private static readonly IReadOnlyDictionary<string, string[]> DefaultRolePermissions = new Dictionary<string, string[]>
    {
        ["Admin"] =
        [
            RolePermission.ProductsCreate,
            RolePermission.RolesManage,
            RolePermission.AccountEmailLookup,
            RolePermission.BasketRead,
            RolePermission.BasketWrite,
            RolePermission.BasketDelete,
            RolePermission.OrdersCreate,
            RolePermission.OrdersRead
        ],
        ["Customer"] = [RolePermission.BasketRead, RolePermission.BasketWrite, RolePermission.BasketDelete,
            RolePermission.OrdersCreate, RolePermission.OrdersRead]
    };

    public static async Task SeedRolesAndAdminAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        AppIdentityDbContext identityDbContext,
        IConfiguration configuration)
    {
        foreach (var roleName in DefaultRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create the required {roleName} role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        foreach (var (roleName, permissions) in DefaultRolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName)
                ?? throw new InvalidOperationException($"The required {roleName} role could not be loaded.");
            var existingPermissions = (await roleManager.GetClaimsAsync(role))
                .Where(claim => claim.Type == RolePermission.ClaimType)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions.Where(permission => !existingPermissions.Contains(permission)))
            {
                var result = await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(RolePermission.ClaimType, permission));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to assign the default permission {permission} to {roleName}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
                }
            }
        }

        foreach (var user in userManager.Users.ToList())
        {
            if ((await userManager.GetRolesAsync(user)).Count != 0)
            {
                continue;
            }

            var addCustomerRoleResult = await userManager.AddToRoleAsync(user, "Customer");
            if (!addCustomerRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign the Customer role to an existing user: {string.Join("; ", addCustomerRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
        if (adminUsers.Count == 0)
        {
            var adminEmail = configuration["BootstrapAdmin:Email"];
            var adminPassword = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "No administrator exists. Configure BootstrapAdmin__Email and BootstrapAdmin__Password using User Secrets or environment variables.");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new AppUser
                {
                    DisplayName = configuration["BootstrapAdmin:DisplayName"] ?? "Administrator",
                    Email = adminEmail,
                    UserName = adminEmail
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create the configured administrator: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
                }
            }

            await using var transaction = await identityDbContext.Database.BeginTransactionAsync();
            var currentRoles = await userManager.GetRolesAsync(adminUser);
            var removeRolesResult = await userManager.RemoveFromRolesAsync(adminUser, currentRoles);
            if (!removeRolesResult.Succeeded)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException(
                    $"Failed to replace the configured administrator's existing roles: {string.Join("; ", removeRolesResult.Errors.Select(error => error.Description))}");
            }

            var addAdminRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!addAdminRoleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException(
                    $"Failed to assign the Admin role to the configured administrator: {string.Join("; ", addAdminRoleResult.Errors.Select(error => error.Description))}");
            }

            await transaction.CommitAsync();
        }
    }
}
