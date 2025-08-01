using ItemManagement.Interface;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System; // Added for Exception
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
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
                return await _orderService.PlaceOrderAsync(orderDto, User);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error placing order: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while placing the order." });
            }
        }

        [HttpPost("accept/{orderId}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> AcceptOrder(Guid orderId)
        {
            try
            {
                var result = await _orderService.AcceptOrderAsync(orderId, User);
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"An unexpected error occurred while accepting the order: {ex.Message}" });
            }
        }

        [HttpDelete("deleteorder/{orderId}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            try
            {
                return await _orderService.DeleteOrderAsync(orderId, User);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error deleting order {orderId}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the order." });
            }
        }

        [HttpGet("myorders")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin,User")]
        public async Task<IActionResult> GetMyOrders()
        {
            return await _orderService.GetMyOrdersAsync(User);
        }

        [HttpGet("storeorders")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> GetStoreOrders()
        {
            return await _orderService.GetStoreOrdersAsync(User);
        }

        [HttpGet("allorders")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAllOrders()
        {
            return await _orderService.GetAllOrdersAsync();
        }

        [HttpPost("reject/{orderId}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> RejectOrder(Guid orderId)
        {
            try
            {
                var result = await _orderService.RejectOrderAsync(orderId, User);
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"An unexpected error occurred while rejecting the order: {ex.Message}" });
            }
        }
    }
}