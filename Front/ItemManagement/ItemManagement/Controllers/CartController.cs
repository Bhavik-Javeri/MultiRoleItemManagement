using ItemManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ItemManagement.Controllers
{
    [Authorize(Roles = "User")]  // Only authenticated users can access cart
    public class CartController : Controller
    {
        private readonly string _baseUrl = "https://localhost:7060/api/";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CartController> _logger;

        public CartController(IHttpClientFactory httpClientFactory, ILogger<CartController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient GetAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(Guid id, decimal price, int quantity)
        {
            try
            {
                var client = GetAuthorizedClient();
                var cartData = new { ItemId = id, Price = price, Quantity = quantity };


                var content = new StringContent(
                    JsonSerializer.Serialize(cartData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    Encoding.UTF8,
                    "application/json"
                );

                var addItemUrl = $"{_baseUrl}Cart/additem";
                var response = await client.PostAsync(addItemUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Item added to cart successfully!";
                    return RedirectToAction(nameof(CartItems));
                }

                TempData["Error"] = "Could not add item to cart. Please try again.";
                return RedirectToAction(nameof(CartItems));
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not add item to cart. Please try again.";
                return RedirectToAction(nameof(CartItems));
            }
        }
        [HttpGet]
        public async Task<IActionResult> CartItems()
        {
            try
            {
                var client = GetAuthorizedClient();
                var getCartItemsUrl = $"{_baseUrl}Cart/items";
                var response = await client.GetAsync(getCartItemsUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("Login", "Auth");
                    }

                    TempData["Error"] = "Could not load your cart items. Please try again.";
                    return View(new List<CartItemViewModel>());
                }

                var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (cartItems == null || !cartItems.Any())
                {
                    ViewBag.EmptyCartMessage = "Your cart is empty.";
                    ViewBag.CartTotal = 0;
                    return View(new List<CartItemViewModel>());
                }

                // Calculate cart total
                decimal cartTotal = cartItems.Sum(item => item.Price * item.Quantity);
                ViewBag.CartTotal = cartTotal;
                return View(cartItems);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not load your cart items. Please try again.";
                return View(new List<CartItemViewModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(Guid itemId)
        {
            try
            {
                var client = GetAuthorizedClient();
                var removeItemUrl = $"{_baseUrl}Cart/removeitem/{itemId}";

                var response = await client.DeleteAsync(removeItemUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Could not remove item from cart. Please try again.";
                }
                else
                {
                    TempData["Success"] = "Item removed from cart successfully.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Could not remove item from cart. Please try again.";
            }

            return RedirectToAction(nameof(CartItems));
        }

        [HttpPost]
        public async Task<IActionResult> IncrementQuantity(Guid itemId)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var apiUrl = $"{_baseUrl}api/cart/incrementquantity/{itemId}";
                var response = await client.PostAsync(apiUrl, null);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    // Expecting backend to return JSON with success, quantity, price, cartTotal
                    return Content(content, "application/json");
                }
                else
                {
                    return Json(new { success = false, message = "Failed to increment quantity." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Error incrementing quantity." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DecrementQuantity(Guid itemId)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var apiUrl = $"{_baseUrl}api/cart/decrementquantity/{itemId}";
                var response = await client.PostAsync(apiUrl, null);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    // Expecting backend to return JSON with success, quantity, price, cartTotal
                    return Content(content, "application/json");
                }
                else
                {
                    return Json(new { success = false, message = "Failed to decrement quantity." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Error decrementing quantity." });
            }
        }
    }
}
