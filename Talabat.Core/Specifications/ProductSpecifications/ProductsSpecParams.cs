using System.ComponentModel.DataAnnotations;

namespace Talabat.Core.Specifications.ProductSpecifications;

public class ProductsSpecParams
{
    [StringLength(10)]
    [RegularExpression("^(priceAsc|priceDesc)?$")]
    public string? sort { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 5;

    [Range(1, 100000)]
    public int PageIndex { get; set; } = 1;

    private string? _search;

    [StringLength(100)]
    public string? Search
    {
        get => _search;
        set => _search = value?.Trim().ToLowerInvariant();
    }
}
