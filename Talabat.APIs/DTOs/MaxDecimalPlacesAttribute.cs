using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MaxDecimalPlacesAttribute(int places) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null ||
        value is decimal amount && amount == decimal.Round(amount, places);
}
