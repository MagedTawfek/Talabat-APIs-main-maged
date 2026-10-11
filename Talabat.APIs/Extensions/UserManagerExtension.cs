using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models.Identity;

namespace Talabat.APIs.Extensions;

public static class UserManagerExtension
{
    public static async Task<AppUser?> FindUserWithAddressAsync(this UserManager<AppUser> userManager, ClaimsPrincipal User)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return userId is null ? null : await userManager.Users.Include(x => x.Address).SingleOrDefaultAsync(x => x.Id == userId);
        
    }
}
