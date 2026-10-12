using AutoMapper;
using Microsoft.Extensions.Configuration;
using Talabat.APIs.DTOs;
using Talabat.Core.Models;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Models.Identity;
using Address = Talabat.Core.Models.Order_Aggregate.Address;
using IdentityAddress = Talabat.Core.Models.Identity.Address;

namespace Talabat.APIs.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Core.Models.Product, DTOs.ProductToReturnDto>()
            .ForMember(d => d.Brand, o => o.MapFrom(s => s.Brand.Name))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.PictureUrl, o => o.MapFrom<ProductPictureUrlResolver>());

        CreateMap<AddressDto, Address>().ReverseMap();
        CreateMap<AddressDto, IdentityAddress>()
            .ForMember(destination => destination.FName, options => options.MapFrom(source => source.FirstName))
            .ForMember(destination => destination.LName, options => options.MapFrom(source => source.LastName))
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.AppUserId, options => options.Ignore())
            .ReverseMap()
            .ForMember(destination => destination.FirstName, options => options.MapFrom(source => source.FName))
            .ForMember(destination => destination.LastName, options => options.MapFrom(source => source.LName));
        CreateMap<Order, OrderToReturnDto>()
            .ForMember(d => d.DeliveryMethod, o => o.MapFrom(s => s.DeliveryMethod.ShortName))
            .ForMember(d => d.DeliveryMethodCost, o => o.MapFrom(s => s.DeliveryMethod.Cost))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.OrderItems))
            .ForMember(d => d.Total, o => o.MapFrom(s => s.GetTotal()));
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.Product.ProductId))
            .ForMember(d => d.PictureUrl, o => o.MapFrom(s => s.Product.PictureUrl))
            .ForMember(d => d.PictureUrl, o => o.MapFrom<OrderItemPicsUrlResolver>());
    }
}
