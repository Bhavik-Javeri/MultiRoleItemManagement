using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ItemManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public OrderService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // Read-only methods do not require transactions
        public async Task<IActionResult> GetMyOrdersAsync(ClaimsPrincipal userClaims)
        {
            var userId = GetUserId(userClaims);
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.StoreId,
                    o.Address,
                    o.Pincode,
                    Items = o.OrderItems.Select(oi => new { oi.ItemId, ItemName = oi.Item.Name, oi.Quantity, oi.Price })
                })
                .ToListAsync();
            return new OkObjectResult(orders);
        }

        public async Task<IActionResult> GetStoreOrdersAsync(ClaimsPrincipal userClaims)
        {
            var userStoreId = userClaims.FindFirst("StoreId")?.Value;
            if (string.IsNullOrEmpty(userStoreId))
                return new ForbidResult();

            var storeGuid = Guid.Parse(userStoreId);
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.User)
                .Where(o => o.StoreId == storeGuid)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.UserId,
                    UserName = o.User != null ? o.User.FName + " " + o.User.LName : "N/A",
                    o.Address,
                    o.Pincode,
                    Items = o.OrderItems.Select(oi => new { oi.ItemId, ItemName = oi.Item.Name, oi.Quantity, oi.Price })
                })
                .ToListAsync();
            return new OkObjectResult(orders);
        }

        public async Task<IActionResult> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Include(o => o.User)
                .Include(o => o.Store)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.UserId,
                    o.StoreId,
                    o.Address,
                    o.Pincode,
                    StoreName = o.Store != null ? o.Store.Name : "N/A",
                    UserName = o.User != null ? o.User.FName + " " + o.User.LName : "N/A",
                    Items = o.OrderItems.Select(oi => new { oi.ItemId, ItemName = oi.Item.Name, oi.Quantity, oi.Price })
                })
                .ToListAsync();
            return new OkObjectResult(orders);
        }

        private Guid GetUserId(ClaimsPrincipal userClaims)
        {
            var userIdString = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                throw new ArgumentNullException("User ID is null or empty in the provided claims.");
            }

            return Guid.Parse(userIdString);
        }

        private string GenerateBill(Order order)
        {
            var billDetails = $"Bill for Order ID: {order.OrderId}\n" +
                              $"User ID: {order.UserId}\n" +
                              $"Store ID: {order.StoreId}\n" +
                              $"Order Date: {order.OrderDate}\n" +
                              $"Total Amount:{order.TotalAmount}\n";

            billDetails += $"Total Amount: {order.TotalAmount}\n";
            return billDetails;
        }

        // Helper to generate a detailed HTML order summary for emails
        private async Task<string> GenerateOrderSummaryHtmlAsync(Order order)
        {
            // Load related data if not already loaded
            await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
            await _context.Entry(order).Reference(o => o.Store).LoadAsync();
            await _context.Entry(order).Reference(o => o.User).LoadAsync();
            foreach (var oi in order.OrderItems)
            {
                await _context.Entry(oi).Reference(x => x.Item).LoadAsync();
            }

            var store = order.Store;
            var user = order.User;
            var itemsTable = "<table style='border-collapse:collapse;width:100%;'>" +
                "<tr style='background:#f2f2f2;'><th style='border:1px solid #ddd;padding:8px;'>Item</th><th style='border:1px solid #ddd;padding:8px;'>Qty</th><th style='border:1px solid #ddd;padding:8px;'>Price</th><th style='border:1px solid #ddd;padding:8px;'>Subtotal</th></tr>";
            foreach (var oi in order.OrderItems)
            {
                itemsTable += $"<tr>"
                    + $"<td style='border:1px solid #ddd;padding:8px;'>{oi.Item?.Name}</td>"
                    + $"<td style='border:1px solid #ddd;padding:8px;text-align:center;'>{oi.Quantity}</td>"
                    + $"<td style='border:1px solid #ddd;padding:8px;text-align:right;'>₹{oi.Price:F2}</td>"
                    + $"<td style='border:1px solid #ddd;padding:8px;text-align:right;'>₹{(oi.Price * oi.Quantity):F2}</td>"
                    + "</tr>";
            }
            itemsTable += $"<tr style='font-weight:bold;'><td colspan='3' style='border:1px solid #ddd;padding:8px;text-align:right;'>Total</td><td style='border:1px solid #ddd;padding:8px;text-align:right;'>₹{order.TotalAmount:F2}</td></tr>";
            itemsTable += "</table>";

            var html = $@"
                <div style='font-family:sans-serif;'>
                    <h2 style='color:#2d7ff9;'>Order Summary</h2>
                    <p><b>Order ID:</b> {order.OrderId}<br/>
                    <b>Order Date:</b> {order.OrderDate:yyyy-MM-dd HH:mm}<br/>
                    <b>Status:</b> {order.Status}<br/>
                    <b>Customer:</b> {user?.FName} {user?.LName} ({user?.Email})<br/>
                    <b>Store:</b> {store?.Name} ({store?.Email}, {store?.ContactNumber})<br/>
                    <b>Delivery Address:</b> {order.Address}, {order.Pincode}</p>
                    {itemsTable}
                </div>";
            return html;
        }

        public async Task<IActionResult> PlaceOrderAsync(OrderDto orderDto, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var userId = GetUserId(userClaims);
                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult("Cart not found for this user.");
                }

                var store = await _context.Stores.FindAsync(orderDto.StoreId);
                if (store == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult("Store not found.");
                }

                var orderItems = await _context.CartItems.Where(ci => ci.CartId == cart.Id)
                .Select(ci => new OrderItem
                {
                    ItemId = ci.ItemId,
                    Quantity = ci.Quantity,
                    Price = ci.Price
                }).ToListAsync();

                if (!orderItems.Any())
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult("Cart is empty. Cannot place an order with no items.");
                }

                var itemIds = orderItems.Select(oi => oi.ItemId).ToList();
                var itemsInDb = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();
                foreach (var orderItem in orderItems)
                {
                    var item = itemsInDb.FirstOrDefault(i => i.Id == orderItem.ItemId);
                    if (item == null)
                    {
                        transaction.Rollback();
                        return new BadRequestObjectResult($"Item with ID {orderItem.ItemId} not found.");
                    }
                    if (orderItem.Quantity > item.Quantity)
                    {
                        transaction.Rollback();
                        return new BadRequestObjectResult($"You cannot order more than available quantity for item: {item.Name}. Available: {item.Quantity}, Requested: {orderItem.Quantity}");
                    }
                }

                foreach (var orderItem in orderItems)
                {
                    var item = itemsInDb.First(i => i.Id == orderItem.ItemId);
                    item.Quantity -= orderItem.Quantity;
                    _context.Items.Update(item);
                }

                var totalAmount = orderItems.Sum(oi => oi.Price * oi.Quantity);
                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    UserId = userId,
                    StoreId = orderDto.StoreId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    Address = orderDto.Address,
                    Pincode = orderDto.Pincode
                };

                foreach (var orderItem in orderItems)
                {
                    orderItem.OrderId = order.OrderId;
                }

                _context.Orders.Add(order);
                _context.OrderItems.AddRange(orderItems);

                _context.CartItems.RemoveRange(_context.CartItems.Where(ci => ci.CartId == cart.Id));
                _context.Carts.Remove(cart);

                await _context.SaveChangesAsync(); // Save all changes within the transaction

                // Email notifications are external operations, not part of the DB transaction.
                // They are triggered after the DB commit.
                var storeAdmins = await _context.Users.Where(u => u.Role == "StoreAdmin" && u.StoreId == order.StoreId && u.IsActive).ToListAsync();
                var superAdmins = await _context.Users.Where(u => u.Role == "SuperAdmin" && u.IsActive).ToListAsync();
                var allAdmins = new List<User>();
                allAdmins.AddRange(storeAdmins);
                allAdmins.AddRange(superAdmins);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                string orderInfo = $"<p>Order placed by: {user?.FName} {user?.LName} ({user?.Email})</p>";
                var orderSummaryHtml = await GenerateOrderSummaryHtmlAsync(order);
                foreach (var admin in allAdmins)
                {
                    await _emailSender.SendEmailAsync(admin.Email, "New Order Placed", orderInfo + orderSummaryHtml);
                }

                if (user != null)
                {
                    string subject = "Order Placed Successfully";
                    string body = $@"<p>Dear {user.FName},</p><p>Your order has been placed successfully. We will notify you once it is approved or rejected.</p>" + await GenerateOrderSummaryHtmlAsync(order);
                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                }

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult("Order placed successfully.");
            }
            catch (ArgumentNullException ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error placing order (ArgumentNullException): {ex.Message}");
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any general error
                Console.WriteLine($"Error placing order: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> AcceptOrderAsync(Guid orderId, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                Console.WriteLine($"AcceptOrderAsync called with orderId: {orderId}");
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Order not found with ID: {orderId}");
                    return new NotFoundObjectResult("Order not found.");
                }

                var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
                Console.WriteLine($"User role: {userRole}");

                if (userRole == "StoreAdmin")
                {
                    var userStoreId = userClaims.FindFirst("StoreId")?.Value;
                    Console.WriteLine($"StoreAdmin's StoreId: {userStoreId}, Order's StoreId: {order.StoreId}");
                    if (userStoreId == null || order.StoreId.ToString() != userStoreId)
                    {
                        transaction.Rollback();
                        return new ForbidResult();
                    }
                }

                Console.WriteLine($"Current order status: {order.Status}");
                order.Status = OrderStatus.Approved;
                Console.WriteLine($"Updated order status to: {order.Status}");

                await _context.SaveChangesAsync(); // Save changes within the transaction
                Console.WriteLine("Changes saved to database");

                // Email notification is an external operation, triggered after DB commit.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null)
                {
                    string subject = "Your Order Has Been Approved";
                    string body = $@"<p>Dear {user.FName},</p><p>Your order has been approved. Thank you for shopping with us!</p>" + await GenerateOrderSummaryHtmlAsync(order);
                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                }

                await transaction.CommitAsync(); // Commit transaction on success
                var bill = GenerateBill(order);
                return new OkObjectResult(new { Message = "Order approved and bill generated.", OrderId = order.OrderId, Bill = bill });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                Console.WriteLine($"Error accepting order: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteOrderAsync(Guid orderId, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult(new { Message = "Order not found." });
                }
                var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "StoreAdmin")
                {
                    var userStoreId = userClaims.FindFirst("StoreId")?.Value;
                    if (userStoreId == null || order.StoreId.ToString() != userStoreId)
                    {
                        transaction.Rollback();
                        return new ForbidResult();
                    }
                }
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Order deleted successfully." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                Console.WriteLine($"Error deleting order: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> RejectOrderAsync(Guid orderId, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                Console.WriteLine($"RejectOrderAsync called with orderId: {orderId}");
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Order not found with ID: {orderId}");
                    return new NotFoundObjectResult("Order not found.");
                }

                var userRole = userClaims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                Console.WriteLine($"User role: {userRole}");

                if (userRole == "StoreAdmin")
                {
                    var userStoreId = userClaims.FindFirst("StoreId")?.Value;
                    Console.WriteLine($"StoreAdmin's StoreId: {userStoreId}, Order's StoreId: {order.StoreId}");
                    if (userStoreId == null || order.StoreId.ToString() != userStoreId)
                    {
                        transaction.Rollback();
                        return new ForbidResult();
                    }
                }

                Console.WriteLine($"Current order status: {order.Status}");
                order.Status = OrderStatus.Rejected;
                Console.WriteLine($"Updated order status to: {order.Status}");

                await _context.SaveChangesAsync(); // Save changes within the transaction
                Console.WriteLine("Changes saved to database");

                // Email notification is an external operation, triggered after DB commit.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null)
                {
                    string subject = "Your Order Has Been Rejected";
                    string body = $@"<p>Dear {user.FName},</p><p>Your order has been rejected. Please contact the store for more information.</p>" + await GenerateOrderSummaryHtmlAsync(order);
                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                }

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Order rejected successfully.", OrderId = order.OrderId });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                Console.WriteLine($"Error rejecting order: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}