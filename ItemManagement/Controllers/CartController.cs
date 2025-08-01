using ItemManagement.Interface;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCart()
        {
            try
            {
                return await _cartService.CreateCart(User);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error creating cart: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the cart." });
            }
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetCartItems()
        {
            try
            {
                return await _cartService.GetCartItems(User);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                return StatusCode(500, new { Message = "An unexpected error occurred while fetching cart items." });
            }
        }

      [HttpPost("additem")]
public async Task<IActionResult> AddItemToCart([FromBody] CartItemDto cartItemDto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        return await _cartService.AddItemToCart(cartItemDto, User);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "An unexpected error occurred while adding/updating the cart item." });
    }
}

[HttpDelete("removeitem/{itemId}")]
public async Task<IActionResult> RemoveItemFromCart(Guid itemId)
{
    try
    {
        return await _cartService.RemoveItemFromCart(itemId, User);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "An unexpected error occurred while removing the cart item." });
    }
}

[HttpPost("incrementquantity/{itemId}")]
public async Task<IActionResult> IncrementQuantity(Guid itemId)
{
    try
    {
        return await _cartService.IncrementCartItemQuantity(itemId, User);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "An unexpected error occurred while incrementing the quantity." });
    }
}

[HttpPost("decrementquantity/{itemId}")]
public async Task<IActionResult> DecrementQuantity(Guid itemId)
{
    try
    {
        return await _cartService.DecrementCartItemQuantity(itemId, User);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "An unexpected error occurred while decrementing the quantity." });
    }
}

[HttpPost("placeorder")]
[Authorize(Roles = "User")]
public async Task<IActionResult> PlaceOrder([FromBody] OrderDto orderDto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        return await _cartService.PlaceOrder(orderDto, User);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "An unexpected error occurred while placing the order." });
    }
}
    }
}