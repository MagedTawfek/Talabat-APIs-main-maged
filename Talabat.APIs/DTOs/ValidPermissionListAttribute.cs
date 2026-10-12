using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Talabat.APIs.DTOs;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ValidPermissionListAttribute : ValidationAttribute
{
    private static readonly Regex PermissionPattern = new(
        "^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public override bool IsValid(object? value)
    {
        if (value is not IEnumerable<string> permissions)
            return false;

        var values = permissions.ToArray();
        return values.Length <= 32 && values.All(permission =>
            !string.IsNullOrWhiteSpace(permission) &&
            permission.Length <= 128 &&
            PermissionPattern.IsMatch(permission.Trim()));
    }
}
