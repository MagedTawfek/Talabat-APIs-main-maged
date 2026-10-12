using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Talabat.Core.Models.Identity;

namespace Talabat.APIs.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    private const string PolicyPrefix = "permission:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[PolicyPrefix.Length..];
        if (string.IsNullOrWhiteSpace(permission))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(RolePermission.ClaimType, permission)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
