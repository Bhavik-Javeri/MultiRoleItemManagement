using ItemManagement.Models;
using ItemManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ItemManagement.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly string _baseUrl = "https://localhost:7060/api/";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IHttpClientFactory httpClientFactory, ILogger<OrderController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        #region View Orders
        // This section for viewing/listing orders remains the same and is correct.
        public async Task<IActionResult> ListOrders()
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null) { TempData["Error"] = error; return View(new List<OrderViewModel>()); }
            string url = User.IsInRole("SuperAdmin") ? $"{_baseUrl}Order/allorders" : $"{_baseUrl}Order/storeorders";
            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) { TempData["Error"] = $"Failed to load orders. API returned: {response.ReasonPhrase}"; return View(new List<OrderViewModel>()); }
                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<OrderViewModel>>(responseContent, options);
                return View(orders ?? new List<OrderViewModel>());
            }
            catch (Exception ex) { _logger.LogError($"Error in ListOrders: {ex.Message}"); TempData["Error"] = "An error occurred while loading orders."; return View(new List<OrderViewModel>()); }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> MyOrders()
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null) { TempData["Error"] = error; return View(new List<OrderViewModel>()); }
            var url = $"{_baseUrl}Order/myorders";
            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) { TempData["Error"] = $"Failed to load your orders. API returned: {response.ReasonPhrase}"; return View(new List<OrderViewModel>()); }
                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<OrderViewModel>>(responseContent, options);
                if (TempData["OrderSuccess"] != null) { ViewBag.OrderSuccess = (bool)TempData["OrderSuccess"]; }
                return View(orders ?? new List<OrderViewModel>());
            }
            catch (Exception ex) { _logger.LogError($"Error in MyOrders: {ex.Message}"); TempData["Error"] = "An error occurred while loading your orders."; return View(new List<OrderViewModel>()); }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(Guid id)
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null) { return Json(new { success = false, message = error }); }
            string listUrl = User.IsInRole("User") ? $"{_baseUrl}Order/myorders" : $"{_baseUrl}Order/allorders";
            var response = await client.GetAsync(listUrl);
            if (!response.IsSuccessStatusCode) { return Json(new { success = false, message = "Could not fetch order details." }); }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var orders = JsonSerializer.Deserialize<List<OrderViewModel>>(content, options);
            var order = orders?.FirstOrDefault(o => o.Id == id);
            if (order == null) { return Json(new { success = false, message = "Order not found." }); }
            return PartialView("_OrderDetailsPartial", order);
        }
        #endregion

        #region Manage Orders
        // This section for approving/rejecting orders remains the same and is correct.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> ApproveOrder(Guid id)
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null) { TempData["ErrorMessage"] = error; return RedirectToAction(nameof(ListOrders)); }
            var url = $"{_baseUrl}Order/accept/{id}";
            var response = await client.PostAsync(url, null);
            if (response.IsSuccessStatusCode) { TempData["SuccessMessage"] = "Order approved successfully."; }
            else { var responseContent = await response.Content.ReadAsStringAsync(); TempData["ErrorMessage"] = $"Failed to approve order: {responseContent}"; }
            return RedirectToAction(nameof(ListOrders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> RejectOrder(Guid id)
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null) { TempData["ErrorMessage"] = error; return RedirectToAction(nameof(ListOrders)); }
            var url = $"{_baseUrl}Order/reject/{id}";
            var response = await client.PostAsync(url, null);
            if (response.IsSuccessStatusCode) { TempData["SuccessMessage"] = "Order rejected successfully."; }
            else { var responseContent = await response.Content.ReadAsStringAsync(); TempData["ErrorMessage"] = $"Failed to reject order: {responseContent}"; }
            return RedirectToAction(nameof(ListOrders));
        }
        #endregion

        // *** THIS IS THE CORRECTED SECTION ***
        #region Place Order
        [HttpGet]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> PlaceOrderForm()
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Index", "Home");
            }

            // We must calculate the cart total to display on the form.
            await RecalculateCartTotal(client);
            if (TempData.ContainsKey("Error"))
            {
                // Redirect if the cart is empty or there was an error.
                return RedirectToAction("Index", "Cart");
            }

            return View(new OrderViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(OrderViewModel model)
        {
            var (client, error) = await GetAuthenticatedHttpClientAsync();
            if (client == null)
            {
                TempData["Error"] = error;
                // We still need the cart total if we return to the view.
                await RecalculateCartTotal(null); // Passing null is fine here
                return View("PlaceOrderForm", model);
            }

            // Because UserName and StoreName are now optional in the ViewModel,
            // the ModelState will be valid for the fields we submitted (Address and Pincode).
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("PlaceOrder failed due to invalid ModelState.");
                await RecalculateCartTotal(client);
                return View("PlaceOrderForm", model);
            }

            var storeIdString = GetClaimValue("StoreId");
            if (string.IsNullOrEmpty(storeIdString) || !Guid.TryParse(storeIdString, out Guid storeId))
            {
                TempData["Error"] = "Could not determine the store for this order. Please try logging in again.";
                await RecalculateCartTotal(client);
                return View("PlaceOrderForm", model);
            }

            // This object matches the backend's OrderDto perfectly.
            var orderData = new
            {
                StoreId = storeId,
                model.Address,
                model.Pincode
            };

            var content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}Order/placeorder";

            try
            {
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["OrderSuccess"] = true;
                    return RedirectToAction("MyOrders");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"API Error placing order: {responseContent}");
                    TempData["Error"] = $"Error placing the order: A server error occurred.";
                    await RecalculateCartTotal(client);
                    return View("PlaceOrderForm", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during PlaceOrder: {ex.Message}");
                TempData["Error"] = "A network error occurred while placing your order.";
                await RecalculateCartTotal(client);
                return View("PlaceOrderForm", model);
            }
        }
        #endregion

        #region Helpers
        private async Task<(HttpClient, string)> GetAuthenticatedHttpClientAsync()
        {
            var token = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(token))
            {
                return (null, "User session not found. Please log in again.");
            }
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, null);
        }

        private async Task RecalculateCartTotal(HttpClient client)
        {
            // If client is null, try to get a new one.
            if (client == null)
            {
                var (newClient, error) = await GetAuthenticatedHttpClientAsync();
                if (newClient == null) { ViewBag.CartTotal = 0m; TempData["Error"] = error; return; }
                client = newClient;
            }

            try
            {
                var getCartItemsUrl = $"{_baseUrl}Cart/items";
                var response = await client.GetAsync(getCartItemsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(jsonResponse, options);
                    if (cartItems != null && cartItems.Any())
                    {
                        ViewBag.CartTotal = cartItems.Sum(item => item.Price * item.Quantity);
                    }
                    else
                    {
                        ViewBag.CartTotal = 0m;
                        TempData["Error"] = "Your cart is empty. Please add items before placing an order.";
                    }
                }
                else { ViewBag.CartTotal = 0m; TempData["Error"] = "Unable to load cart information."; }
            }
            catch { ViewBag.CartTotal = 0m; }
        }

        private string GetClaimValue(string claimType)
        {
            // A simpler way to get a claim value from the User object
            return User.FindFirst(claimType)?.Value;
        }
        #endregion
    }
}
