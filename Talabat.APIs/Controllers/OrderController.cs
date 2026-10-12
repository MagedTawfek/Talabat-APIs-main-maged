using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.Core.Models;
using Talabat.Core.Models.Identity;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using Address = Talabat.Core.Models.Order_Aggregate.Address;
using IdentityAddress = Talabat.Core.Models.Identity.Address;

namespace Talabat.APIs.Controllers
{
    public class OrderController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly AppIdentityDbContext _identityDbContext;
        private string? BuyerEmail => User.FindFirstValue(ClaimTypes.Email);
        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public OrderController(IOrderService orderService,
        IMapper mapper,
        AppIdentityDbContext identityDbContext)
        {
            _orderService = orderService;
            _mapper = mapper;
            _identityDbContext = identityDbContext;
        }
        [Authorize(Policy = "permission:" + RolePermission.OrdersCreate)]
        [ProducesResponseType(typeof(OrderToReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]

        [HttpPost]
        public async Task<ActionResult<OrderToReturnDto>> CreateOrder(OrderDto orderDto)
        {
            var userId = UserId;
            var buyerEmail = BuyerEmail;
            if (string.IsNullOrWhiteSpace(buyerEmail) || string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new ApiResponse(401));

            var savedAddress = await _identityDbContext.Set<IdentityAddress>()
                .SingleOrDefaultAsync(address => address.Id == orderDto.AddressId && address.AppUserId == userId);
            if (savedAddress is null)
                return NotFound(new ApiResponse(404, "Address was not found for the signed-in user."));

            var address = new Address(savedAddress.FName, savedAddress.LName, savedAddress.Street,
                savedAddress.City, savedAddress.Country);

            var result = await _orderService.CreateOrderAsync(buyerEmail, userId, orderDto.BasketId, address);

            if (!result.Succeeded)
                return StatusCode(result.StatusCode, new ApiResponse(result.StatusCode, result.ErrorMessage));

            return Ok(_mapper.Map<Order, OrderToReturnDto>(result.Order!));

        }
        [Authorize(Policy = "permission:" + RolePermission.OrdersRead)]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDto>>> GetOrdersForUser()
        {
            var buyerEmail = BuyerEmail;
            if (string.IsNullOrWhiteSpace(buyerEmail)) return Unauthorized(new ApiResponse(401));

            var orders = await _orderService.GetOrdersForUserAsync(buyerEmail);
            return Ok(_mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(orders));
        }
        [Authorize(Policy = "permission:" + RolePermission.OrdersRead)]
        [ProducesResponseType(typeof(OrderToReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderToReturnDto>> GetOrderById(int id)
        {
            var buyerEmail = BuyerEmail;
            if (string.IsNullOrWhiteSpace(buyerEmail)) return Unauthorized(new ApiResponse(401));

            var order = await _orderService.GetOrderByIdForUserAsync(id, buyerEmail);
            if (order is null) return NotFound(new ApiResponse(404));
            return Ok(_mapper.Map<Order,OrderToReturnDto>(order));
        }
    }
}
