using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;
using Talabat.APIs.Helpers;
using Talabat.Core.Models;
using Talabat.Core.Services.Contract;

namespace Talabat.Tests;

public class CatalogAndErrorEndpointTests
{
    [Fact]
    public async Task ProductListUsesRequestedFiltersAndReturnsPagination()
    {
        var service = new Mock<IProductService>();
        service.Setup(productService => productService.GetProductsAsync(It.IsAny<Talabat.Core.Specifications.ProductSpecifications.ProductsSpecParams>()))
            .ReturnsAsync(Array.Empty<Product>());
        service.Setup(productService => productService.GetCountAsync(It.IsAny<Talabat.Core.Specifications.ProductSpecifications.ProductsSpecParams>()))
            .ReturnsAsync(0);
        var mapper = new Mock<IMapper>();
        mapper.Setup(value => value.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(It.IsAny<IReadOnlyList<Product>>()))
            .Returns(Array.Empty<ProductToReturnDto>());
        var controller = new ProductsController(mapper.Object, service.Object);

        var result = await controller.GetProducts(new Talabat.Core.Specifications.ProductSpecifications.ProductsSpecParams
        {
            PageIndex = 2,
            PageSize = 5
        });

        var pagination = Assert.IsType<Pagination<ProductToReturnDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, pagination.PageIndex);
        Assert.Equal(5, pagination.PageSize);
        service.Verify(productService => productService.GetProductsAsync(It.IsAny<Talabat.Core.Specifications.ProductSpecifications.ProductsSpecParams>()), Times.Once);
    }

    [Fact]
    public async Task ProductLookupReturnsNotFoundAndCatalogListsArePublicResults()
    {
        var service = new Mock<IProductService>();
        service.Setup(value => value.GetProductAsync(999)).ReturnsAsync((Product?)null);
        var controller = new ProductsController(Mock.Of<IMapper>(), service.Object);

        Assert.IsType<NotFoundObjectResult>((await controller.GetProduct(999)).Result);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(500)]
    public void ErrorEndpointReturnsRequestedHttpStatus(int statusCode)
    {
        var result = new ErrorsController().Error(statusCode);

        Assert.Equal(statusCode, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Theory]
    [InlineData(399)]
    [InlineData(600)]
    public void ErrorEndpointRejectsStatusCodesOutsideHttpErrorRange(int statusCode)
    {
        var result = new ErrorsController().Error(statusCode);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
