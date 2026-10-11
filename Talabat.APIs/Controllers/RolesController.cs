using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.APIs.Authorization;
using Talabat.Core.Models.Identity;
using Talabat.Repository.Identity;

namespace Talabat.APIs.Controllers;

[Authorize(Roles = "Admin")]
public sealed class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    AppIdentityDbContext identityDbContext) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            roleDtos.Add(await ToRoleDtoAsync(role));
        }

        return Ok(roleDtos);
    }

    [HttpGet("{roleName}")]
    public async Task<ActionResult<RoleDto>> GetRole(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        return role is null ? NotFound(new ApiResponse(StatusCodes.Status404NotFound, "Role not found.")) :
            Ok(await ToRoleDtoAsync(role));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto request)
    {
        var roleName = request.Name.Trim();
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return Conflict(new ApiResponse(StatusCodes.Status409Conflict, "Role already exists."));
        }

        var role = new IdentityRole(roleName);
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = result.Errors.Select(error => error.Description)
            });
        }

        var permissionResult = await AddPermissionsAsync(role, request.Permissions);
        if (!permissionResult.Succeeded)
        {
            await roleManager.DeleteAsync(role);
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = permissionResult.Errors.Select(error => error.Description)
            });
        }

        var response = await ToRoleDtoAsync(role);
        return CreatedAtAction(nameof(GetRole), new { roleName = role.Name }, response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{roleName}/permissions")]
    public async Task<ActionResult<RoleDto>> AddPermission(string roleName, UpdateRolePermissionDto request)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null || role.Name is null)
        {
            return NotFound(new ApiResponse(StatusCodes.Status404NotFound, "Role not found."));
        }

        var result = await AddPermissionsAsync(role, [request.Permission]);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = result.Errors.Select(error => error.Description)
            });
        }

        return Ok(await ToRoleDtoAsync(role));
    }

    private async Task<IdentityResult> AddPermissionsAsync(IdentityRole role, IEnumerable<string> permissions)
    {
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var existing = currentClaims
            .Where(claim => claim.Type == RolePermission.ClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Permissions cannot be empty." });
            }

            if (existing.Add(permission))
            {
                var result = await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(RolePermission.ClaimType, permission));
                if (!result.Succeeded)
                {
                    return result;
                }
            }
        }

        return IdentityResult.Success;
    }

    private async Task<RoleDto> ToRoleDtoAsync(IdentityRole role)
    {
        var permissions = await roleManager.GetClaimsAsync(role);
        return new RoleDto(role.Name!, permissions
            .Where(claim => claim.Type == RolePermission.ClaimType)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray());
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{email}/role")]
    public async Task<ActionResult<UserRoleDto>> AssignRole(string email, AssignRoleDto request)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return NotFound(new ApiResponse(StatusCodes.Status404NotFound, "User not found."));
        }

        var role = await roleManager.FindByNameAsync(request.RoleName.Trim());
        if (role is null || role.Name is null)
        {
            return NotFound(new ApiResponse(StatusCodes.Status404NotFound, "Role not found."));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && string.Equals(currentRoles[0], role.Name, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new UserRoleDto(user.Email!, role.Name));
        }

        if (currentRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase) &&
            !string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var administrators = await userManager.GetUsersInRoleAsync("Admin");
            if (administrators.Count <= 1)
            {
                return Conflict(new ApiResponse(StatusCodes.Status409Conflict, "The last administrator cannot be assigned a different role."));
            }
        }

        await using var transaction = await identityDbContext.Database.BeginTransactionAsync();
        var removeRolesResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeRolesResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = removeRolesResult.Errors.Select(error => error.Description)
            });
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, role.Name);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = addRoleResult.Errors.Select(error => error.Description)
            });
        }

        var now = DateTime.UtcNow;
        await identityDbContext.UserRefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now));
        var updateStampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!updateStampResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiValidationErrorResponse
            {
                Errors = updateStampResult.Errors.Select(error => error.Description)
            });
        }

        await transaction.CommitAsync();
        return Ok(new UserRoleDto(user.Email!, role.Name));
    }
}
