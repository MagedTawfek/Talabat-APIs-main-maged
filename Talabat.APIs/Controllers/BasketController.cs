using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;

namespace Talabat.APIs.Controllers
{
  
    [Authorize(Roles = "Customer")]
    public class BasketController : BaseApiController
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IMapper _mapper;

        public BasketController(IBasketRepository basketRepository,IMapper mapper)
        {
            _basketRepository = basketRepository;
            _mapper = mapper;
        }

        private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        [HttpGet]
        public async Task<ActionResult<CustomerBasket>> GetBasket(string id)
        {
            var basket = await _basketRepository.GetBasketAsync(id, OwnerId);
            return Ok(basket ?? new CustomerBasket(id) { OwnerId = OwnerId });
        }
        [HttpPost]
        public async Task<ActionResult<CustomerBasket>> UpdateBasket(CustomerBasketDto basket)
        {
            var mappedBasket = _mapper.Map<CustomerBasketDto, CustomerBasket>(basket);
            var updatedBasket = await _basketRepository.UpdateBasketAsync(mappedBasket, OwnerId);
            if (updatedBasket == null)
            {
                return BadRequest(new ApiResponse(400));
            }
            return Ok(updatedBasket);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<CustomerBasket>> DeleteBasket(string id)
        {
            var deleted = await _basketRepository.DeleteBasketAsync(id, OwnerId);
            if (!deleted)
            {
                return BadRequest(new ApiResponse(400));
            }
            return Ok();
        }
        
    }
}
