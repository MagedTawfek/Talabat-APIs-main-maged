using System.ComponentModel.DataAnnotations.Schema;

namespace Talabat.Core.Models;

public class Product : BaseModel
{
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public string PictureUrl { get; set; }
    
    public decimal Price { get; set; }
    
}
