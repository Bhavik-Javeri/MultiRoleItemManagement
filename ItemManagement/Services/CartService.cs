using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace ItemManagement.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<IActionResult> CreateCart(ClaimsPrincipal userClaims)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetUserId(userClaims);
                var existingCart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

                if (existingCart != null)
                {
                    return new ConflictObjectResult("Cart already exists for this user.");
                }

                var newCart = new Cart { UserId = userId };
                _context.Carts.Add(newCart);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new OkObjectResult("Cart created successfully.");
            }
            catch (ArgumentNullException ex)
            {
                await transaction.RollbackAsync();
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetCartItems(ClaimsPrincipal userClaims)
        {
            try
            {
                var userId = GetUserId(userClaims);

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartId == cart.Id)
                    .Select(ci => new
                    {
                        ci.ItemId,
                        ci.Quantity,
                        ci.Price,
                        ItemName = ci.Item.Name
                    })
                    .ToListAsync();

                return new OkObjectResult(cartItems);
            }
            catch (ArgumentNullException ex)
            {
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> AddItemToCart(CartItemDto cartItemDto, ClaimsPrincipal userClaims)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (cartItemDto.Quantity <= 0 || cartItemDto.Price <= 0)
                {
                    return new BadRequestObjectResult("Quantity and Price must be positive.");
                }

                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == cartItemDto.ItemId);
                if (item == null)
                {
                    return new NotFoundObjectResult("Item not found.");
                }
                if (cartItemDto.Quantity > item.Quantity)
                {
                    return new BadRequestObjectResult($"Only {item.Quantity} available in stock.");
                }

                var userId = GetUserId(userClaims);
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == cartItemDto.ItemId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity = cartItemDto.Quantity;
                    existingCartItem.Price = cartItemDto.Price;
                    _context.CartItems.Update(existingCartItem);
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ItemId = cartItemDto.ItemId,
                        Quantity = cartItemDto.Quantity,
                        Price = cartItemDto.Price
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OkObjectResult("Item added to cart.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> RemoveItemFromCart(Guid itemId, ClaimsPrincipal userClaims)
        {
            var userId = GetUserId(userClaims);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                return new NotFoundObjectResult("Cart not found.");
            }
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);
            if (cartItem == null)
            {
                return new NotFoundObjectResult("Item not found in cart.");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return new OkObjectResult("Item removed from cart.");
        }

        public async Task<IActionResult> IncrementCartItemQuantity(Guid itemId, ClaimsPrincipal userClaims)
        {
            var userId = GetUserId(userClaims);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                return new BadRequestObjectResult(new { success = false, message = "Cart not found." });
            }
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);
            if (cartItem == null)
            {
                return new BadRequestObjectResult(new { success = false, message = "Item not found in cart." });
            }
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null)
            {
                return new BadRequestObjectResult(new { success = false, message = "Item not found." });
            }
            if (cartItem.Quantity < item.Quantity)
            {
                cartItem.Quantity += 1;
                _context.CartItems.Update(cartItem);
                await _context.SaveChangesAsync();
            }
            var total = cartItem.Price * cartItem.Quantity;
            var cartTotal = await _context.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => ci.Price * ci.Quantity);
            return new OkObjectResult(new { success = true, quantity = cartItem.Quantity, price = cartItem.Price, cartTotal = cartTotal });
        }

        public async Task<IActionResult> DecrementCartItemQuantity(Guid itemId, ClaimsPrincipal userClaims)
        {
            var userId = GetUserId(userClaims);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                return new BadRequestObjectResult(new { success = false, message = "Cart not found." });
            }
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);
            if (cartItem == null)
            {
                return new BadRequestObjectResult(new { success = false, message = "Item not found in cart." });
            }
            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity -= 1;
                _context.CartItems.Update(cartItem);
                await _context.SaveChangesAsync();
            }
            var total = cartItem.Price * cartItem.Quantity;
            var cartTotal = await _context.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => ci.Price * ci.Quantity);
            return new OkObjectResult(new { success = true, quantity = cartItem.Quantity, price = cartItem.Price, cartTotal = cartTotal });
        }    

        public async Task<IActionResult> SetCartItemQuantity(Guid itemId, int quantity, ClaimsPrincipal userClaims)
        {
            var userId = GetUserId(userClaims);
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return new NotFoundObjectResult("Cart not found.");

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);
            if (cartItem == null)
                return new NotFoundObjectResult("Item not found in cart.");

            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null)
                return new NotFoundObjectResult("Item not found.");

            if (quantity < 1)
                quantity = 1;
            if (quantity > item.Quantity)
                quantity = item.Quantity;

            cartItem.Quantity = quantity;
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();

            var total = cartItem.Price * cartItem.Quantity;
            var cartTotal = await _context.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => ci.Price * ci.Quantity);
            return new OkObjectResult(new { Quantity = cartItem.Quantity, ItemTotal = total, CartTotal = cartTotal });
        }

        public async Task<IActionResult> PlaceOrder(OrderDto orderDto, ClaimsPrincipal userClaims)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetUserId(userClaims);
                var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    return new NotFoundObjectResult("Cart not found.");
                }

                var cartItems = await _context.CartItems.Where(ci => ci.CartId == cart.Id).ToListAsync();
                if (!cartItems.Any())
                {
                    return new BadRequestObjectResult("Cart is empty.");
                }

                var store = await _context.Stores.FindAsync(orderDto.StoreId);
                if (store == null)
                {
                    return new NotFoundObjectResult("Store not found.");
                }

                var totalAmount = cartItems.Sum(ci => ci.Price * ci.Quantity);

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

                var orderItems = cartItems.Select(ci => new OrderItem
                {
                    OrderId = order.OrderId,
                    ItemId = ci.ItemId,
                    Quantity = ci.Quantity,
                    Price = ci.Price
                }).ToList();

                _context.Orders.Add(order);
                _context.OrderItems.AddRange(orderItems);
                _context.CartItems.RemoveRange(cartItems);
                _context.Carts.Remove(cart);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    Message = "Order placed successfully.",
                    OrderId = order.OrderId,
                    TotalAmount = order.TotalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error placing order: {ex}");
                return new StatusCodeResult(500);
            }
        }
    }
}
