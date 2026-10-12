using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.APIs.Helpers;
using Talabat.Core.Models.Identity;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;
using Talabat.Core.Specification;
using Talabat.Core.Specifications.ProductSpecifications;

namespace Talabat.APIs.Controllers;

[Route($"api/[controller]")]
[ApiController] 
public class ProductsController : BaseApiController
{

    private readonly IMapper _mapper;
    private readonly IProductService _productService;

    public ProductsController(IMapper mapper,IProductService productService)
    {
        _mapper = mapper;
        _productService = productService;
    }
    [AllowAnonymous]
    [HttpGet] // api/products
    public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts([FromQuery]ProductsSpecParams specParams)
    {
        var products = await _productService.GetProductsAsync(specParams);
        var count = await _productService.GetCountAsync(specParams) ;                 
        var data  = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);
        return Ok(new Pagination<ProductToReturnDto>(specParams.PageIndex, specParams.PageSize, data, count));
    }
   
    [ProducesResponseType(typeof(ProductToReturnDto), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [AllowAnonymous]
    [HttpGet("{id:int}")] // api/products/1
    public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
    {
        var product = await _productService.GetProductAsync(id);

        if (product == null)
        {
            return NotFound(new ApiResponse(404));
        }
        var productToReturn = _mapper.Map<Product, ProductToReturnDto>(product);

        return Ok(productToReturn); // 200
    }
    
    [Authorize(Policy = "permission:" + RolePermission.ProductsCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(ProductToReturnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductToReturnDto>> CreateProduct([FromBody] CreateProductDto request)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            PictureUrl = request.PictureUrl.Trim(),
            Price = request.Price
        };

        var createdProduct = await _productService.CreateProductAsync(product);
        var response = _mapper.Map<ProductToReturnDto>(createdProduct);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, response);
    }
    
} 
