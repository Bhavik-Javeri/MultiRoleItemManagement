using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ItemManagement.Interface
{
    public interface IOrderService
    {
        Task<IActionResult> PlaceOrderAsync(OrderDto orderDto, ClaimsPrincipal userClaims);
        Task<IActionResult> AcceptOrderAsync(Guid orderId, ClaimsPrincipal userClaims);
        Task<IActionResult> DeleteOrderAsync(Guid orderId, ClaimsPrincipal userClaims);
        Task<IActionResult> GetMyOrdersAsync(ClaimsPrincipal userClaims);
        Task<IActionResult> GetStoreOrdersAsync(ClaimsPrincipal userClaims);
        Task<IActionResult> GetAllOrdersAsync();
        Task<IActionResult> RejectOrderAsync(Guid orderId, ClaimsPrincipal userClaims);
    }
}