using AutoMapper;
using Microsoft.Extensions.Configuration;
using Talabat.APIs.DTOs;
using Talabat.APIs.Helpers;
using Talabat.Core.Models.Order_Aggregate;
using IdentityAddress = Talabat.Core.Models.Identity.Address;
using OrderAddress = Talabat.Core.Models.Order_Aggregate.Address;

namespace Talabat.Tests;

public class AddressMappingTests
{
    [Fact]
    public void AccountAddressMapsBetweenIdentityNamesAndPublicAddressFields()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<MappingProfiles>());
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();

        var dto = mapper.Map<AddressDto>(new IdentityAddress
        {
            Id = 17,
            AppUserId = "user-1",
            FName = "Ada",
            LName = "Lovelace",
            Street = "1 Main St",
            City = "London",
            Country = "UK"
        });
        var address = mapper.Map<IdentityAddress>(new AddressInputDto
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Street = "2 Main St",
            City = "New York",
            Country = "US"
        });

        Assert.Equal("Ada", dto.FirstName);
        Assert.Equal("Lovelace", dto.LastName);
        Assert.Equal(17, dto.Id);
        Assert.Equal("Grace", address.FName);
        Assert.Equal("Hopper", address.LName);
        Assert.Equal(0, address.Id);
        Assert.Null(address.AppUserId);
    }

    [Fact]
    public void OrderResponseContainsAuthoritativeItemsAndTotal()
    {
        var configurationValues = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiBaseUrl"] = "https://api.example.test"
        }).Build();
        var configuration = new MapperConfiguration(config =>
        {
            config.ConstructServicesUsing(type => Activator.CreateInstance(type, configurationValues)!);
            config.AddProfile<MappingProfiles>();
        });
        var order = new Order("ada@example.com", new OrderAddress(),
            [new OrderItem(new ProductItemOrder(7, "Tea", "/tea.png"), 12m, 2)], 24m);

        var response = configuration.CreateMapper().Map<OrderToReturnDto>(order);

        Assert.Equal(24m, response.Total);
        var item = Assert.Single(response.Items);
        Assert.Equal("Tea", item.ProductName);
        Assert.Equal("7", item.ProductId);
        Assert.Equal("https://api.example.test/tea.png", item.PictureUrl);
    }
}
