using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;

namespace Talabat.APIs.Controllers;

[Authorize(Roles = "Customer")]
public class BasketController : BaseApiController
{
    private readonly IBasketRepository _basketRepository;
    private readonly IProductService _productService;

    public BasketController(IBasketRepository basketRepository, IProductService productService)
    {
        _basketRepository = basketRepository;
        _productService = productService;
    }

    private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<CustomerBasket>> GetBasket()
    {
        var basket = await _basketRepository.GetBasketAsync(OwnerId, OwnerId);
        return Ok(basket ?? new CustomerBasket(OwnerId) { OwnerId = OwnerId });
    }

    [HttpPost]
    public async Task<ActionResult<CustomerBasket>> UpdateBasket(CustomerBasketDto basket)
    {
        if (basket.Items is null || basket.Items.Count == 0)
        {
            return BadRequest(new ApiResponse(400, "A basket must contain at least one item."));
        }

        var items = new List<BasketItem>(basket.Items.Count);
        var productIds = new HashSet<int>();
        foreach (var item in basket.Items)
        {
            if (!productIds.Add(item.ProductId))
            {
                return BadRequest(new ApiResponse(400, $"Product {item.ProductId} appears more than once."));
            }

            var product = await _productService.GetProductAsync(item.ProductId);
            if (product?.Brand is null || product.Category is null)
            {
                return NotFound(new ApiResponse(404, $"Product {item.ProductId} was not found."));
            }

            items.Add(new BasketItem
            {
                Id = product.Id,
                productName = product.Name,
                PictureUrl = product.PictureUrl,
                Price = product.Price,
                Category = product.Category.Name,
                Brand = product.Brand.Name,
                Quantity = item.Quantity
            });
        }

        var mappedBasket = new CustomerBasket(OwnerId) { OwnerId = OwnerId, items = items };
        var updatedBasket = await _basketRepository.UpdateBasketAsync(mappedBasket, OwnerId);
        if (updatedBasket is null)
        {
            return BadRequest(new ApiResponse(400));
        }

        return Ok(updatedBasket);
    }

    [HttpDelete]
    public async Task<ActionResult<CustomerBasket>> DeleteBasket()
    {
        var deleted = await _basketRepository.DeleteBasketAsync(OwnerId, OwnerId);
        if (!deleted)
        {
            return NotFound(new ApiResponse(404, "Basket not found."));
        }

        return Ok();
    }
}
