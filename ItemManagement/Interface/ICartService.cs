using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ItemManagement.Interface
{
    public interface ICartService
    {
        Task<IActionResult> CreateCart(ClaimsPrincipal userClaims);
        Task<IActionResult> GetCartItems(ClaimsPrincipal userClaims);
        Task<IActionResult> AddItemToCart(CartItemDto cartItemDto, ClaimsPrincipal userClaims);
        // New: Method to explicitly remove an item from the cart
        Task<IActionResult> RemoveItemFromCart(Guid itemId, ClaimsPrincipal userClaims);
        // New: Method to place an order from the cart
        Task<IActionResult> PlaceOrder(OrderDto orderDto, ClaimsPrincipal userClaims);
        Task<IActionResult> DecrementCartItemQuantity(Guid itemId, ClaimsPrincipal user);
        Task<IActionResult> IncrementCartItemQuantity(Guid itemId, ClaimsPrincipal user);
        Task<IActionResult> SetCartItemQuantity(Guid itemId, int quantity, ClaimsPrincipal user);
    }
}