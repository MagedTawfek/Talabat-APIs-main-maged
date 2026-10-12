namespace Talabat.Core.Models;

public class BasketItem
{
    public int Id { get; set; }
    public string productName { get; set; }
    public string PictureUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
