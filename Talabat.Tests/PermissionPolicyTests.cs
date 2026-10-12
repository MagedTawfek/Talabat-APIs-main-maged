using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Talabat.APIs.Authorization;

namespace Talabat.Tests;

public class PermissionPolicyTests
{
    [Fact]
    public async Task PermissionPolicyAllowsTheAssignedClaimAndRejectsAUserWithoutIt()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync("permission:orders.read");
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);

        var requirement = Assert.Single(policy.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("permission", requirement.ClaimType);
        Assert.Equal(["orders.read"], requirement.AllowedValues);

        var permitted = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "orders.read")], "test")), null);
        await requirement.HandleAsync(permitted);
        Assert.True(permitted.HasSucceeded);

        var roleOnly = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Customer")], "test")), null);
        await requirement.HandleAsync(roleOnly);
        Assert.False(roleOnly.HasSucceeded);
    }
}
