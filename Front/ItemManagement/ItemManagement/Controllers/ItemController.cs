using ItemManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Security.Claims; // For claims
using System.Linq;
using System.Collections.Generic;
using System;
using System.Net.Http.Headers; // For AuthenticationHeaderValue
using System.IdentityModel.Tokens.Jwt; // For JwtSecurityTokenHandler, JwtRegisteredClaimNames
using ItemManagement.FileUpload; // For FileUploadService
using ItemManagement.Helper;   // For StoreLocation

namespace ItemManagement.Controllers
{
    [Authorize] // All actions in this controller will require authentication by default
    public class ItemController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly FileUploadService _fileUploadService; // NEW: Added FileUploadService
        private readonly StoreLocation _storeLocation;       // NEW: Added StoreLocation

        public ItemController(IHttpClientFactory httpClientFactory, IConfiguration configuration,
            FileUploadService fileUploadService, StoreLocation storeLocation) // NEW: Inject services
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _fileUploadService = fileUploadService; // Assign injected service
            _storeLocation = storeLocation;         // Assign injected service
        }

        // NEW HELPER METHOD: Gets an authorized HttpClient for API calls
        private HttpClient GetAuthorizedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient("ItemManagementApiClient");
            var accessToken = User.FindFirst("AccessToken")?.Value;
            if (string.IsNullOrEmpty(accessToken))
            {
                // In a real application, you might redirect to login here,
                // but for an API client, throwing UnauthorizedAccessException is appropriate.
                throw new UnauthorizedAccessException("Access token is missing. Please log in again.");
            }
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        // NEW HELPER METHOD: Extracts logged-in user's GUID from JWT claims
        private Guid GetLoggedInUserId()
        {
            Guid loggedInUserId = Guid.Empty;
            var accessToken = User.FindFirst("AccessToken")?.Value;

            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;

                    if (jsonToken != null)
                    {
                        var nameIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId);
                        if (nameIdClaim != null && Guid.TryParse(nameIdClaim.Value, out loggedInUserId))
                        {
                            // User ID successfully extracted from nameid
                        }
                        else
                        {
                            var subClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                            if (subClaim != null && Guid.TryParse(subClaim.Value, out loggedInUserId))
                            {
                                // User ID successfully extracted from 'sub'
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: Error parsing AccessToken as JWT in GetLoggedInUserId method (ItemController): {ex.Message}");
                }
            }
            return loggedInUserId;
        }

        // NEW HELPER METHOD: Provides SelectListItems for stores
        private async Task<List<SelectListItem>> GetStoresForDropdown()
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync("stores/allstores");
                response.EnsureSuccessStatusCode();
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var stores = await response.Content.ReadFromJsonAsync<List<StoreViewModel>>(options);

                return stores?.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList() ?? new List<SelectListItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: ItemController: Error fetching stores for dropdown: {ex.Message}");
                return new List<SelectListItem>();
            }
        }

        // NEW HELPER METHOD: Provides SelectListItems for item categories
        private List<SelectListItem> GetItemCategoriesForDropdown(Category? selectedCategory = null)
        {
            // Get all values from the ItemManagement.Models.ViewModels.Category enum
            var categories = Enum.GetValues(typeof(Category))
                       .Cast<Category>()
                       .Select(c => new SelectListItem
                       {
                           Value = c.ToString(), // "Veg", "NonVeg", "Egg"
                           Text = c.ToString(),   // Display text
                           Selected = (c == selectedCategory)
                       })
                       .ToList();

            // Remove the explicit insertion of "-- All Categories --" here.
            // The <option value=""> in the Razor view will handle this.
            
            return categories;
        }


        // GET: /Item/Index (or /Item) - List all items with optional filters
        [HttpGet]
        [AllowAnonymous] // Allow unauthenticated users to view the public item list
        // [Authorize(Roles = "SuperAdmin,StoreAdmin,User")] // Use this if you want only authenticated users
        public async Task<IActionResult> Index([FromQuery] ItemFilterViewModel filter)
        {
            var httpClient = GetAuthorizedHttpClient();
            string requestUri = "Item/GetAllItems";
            var queryParams = new List<string>();

            filter.Categories = GetItemCategoriesForDropdown(filter.Category);
            filter.Stores = await GetStoresForDropdown();

            if (filter.Category.HasValue)
            {
                queryParams.Add($"categoryType={filter.Category.Value}");
            }
            if (filter.SelectedStoreId.HasValue && filter.SelectedStoreId != Guid.Empty)
            {
                queryParams.Add($"storeId={filter.SelectedStoreId.Value}");
            }
            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                queryParams.Add($"searchQuery={filter.SearchQuery}");
            }

            // --- USER STORE FILTERING LOGIC ---
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("StoreAdmin"))
                {
                    var userId = GetLoggedInUserId();
                    if (userId != Guid.Empty)
                    {
                        var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true)
                            {
                                queryParams.Add($"storeId={currentUser.StoreId.Value}");
                            }
                        }
                    }
                }
                else if (User.IsInRole("User"))
                {
                    var userId = GetLoggedInUserId();
                    if (userId != Guid.Empty)
                    {
                        var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true)
                            {
                                queryParams.Add($"storeId={currentUser.StoreId.Value}");
                                // Optionally, set filter.SelectedStoreId = currentUser.StoreId.Value;
                            }
                        }
                    }
                }
                // SuperAdmin: no forced filter
            }
            // --- END USER STORE FILTERING LOGIC ---

            if (queryParams.Any())
            {
                requestUri += "?" + string.Join("&", queryParams);
            }

            try
            {
                var response = await httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    var items = await response.Content.ReadFromJsonAsync<List<ItemViewModel>>(options);
                    if (items != null && items.Any() && items.Any(i => string.IsNullOrEmpty(i.StoreName)))
                    {
                        var storesResponse = await httpClient.GetAsync("stores/allstores");
                        if (storesResponse.IsSuccessStatusCode)
                        {
                            var stores = await storesResponse.Content.ReadFromJsonAsync<List<StoreViewModel>>(options);
                            var storeDictionary = stores?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<Guid, string>();
                            foreach (var item in items)
                            {
                                if (storeDictionary.TryGetValue(item.StoreId, out string? storeName))
                                {
                                    item.StoreName = storeName;
                                }
                            }
                        }
                    }
                    filter.Items = items ?? new List<ItemViewModel>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Frontend ItemController: Error fetching items: {response.StatusCode} - {errorContent}");
                    ModelState.AddModelError("", $"Failed to load items: {errorContent}");
                    filter.Items = new List<ItemViewModel>();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", "Network error. Could not connect to the item service.");
                filter.Items = new List<ItemViewModel>();
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError("", "Error processing item data from the server.");
                filter.Items = new List<ItemViewModel>();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred.");
                filter.Items = new List<ItemViewModel>();
            }

            return View(filter);
        }

        // Placeholder for Create (GET action to display form)
        [HttpGet("/Item/Create")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> Create()
        {
            var model = new ItemViewModel();
            ViewBag.Categories = GetItemCategoriesForDropdown();
            if (User.IsInRole("SuperAdmin"))
            {
                ViewBag.Stores = await GetStoresForDropdown();
            }
            else if (User.IsInRole("StoreAdmin"))
            {
                var userId = GetLoggedInUserId();
                var httpClient = GetAuthorizedHttpClient();
                var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        model.StoreId = currentUser.StoreId.Value;
                        ViewBag.Stores = new List<SelectListItem> {
                            new SelectListItem { Value = model.StoreId.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                        };
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Store Admin account is not associated with a store. Cannot create items.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to retrieve Store Admin's store information. Cannot create items.";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(model);
        }

        // Example for Create (POST action to submit form)
        [HttpPost("/Item/Create")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = GetItemCategoriesForDropdown();
                if (User.IsInRole("SuperAdmin"))
                {
                    ViewBag.Stores = await GetStoresForDropdown();
                }
                else if (User.IsInRole("StoreAdmin"))
                {
                    var userId = GetLoggedInUserId();
                    var httpClient = GetAuthorizedHttpClient();
                    var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                        if (currentUser?.StoreId.HasValue == true)
                        {
                            model.StoreId = currentUser.StoreId.Value;
                            ViewBag.Stores = new List<SelectListItem> {
                                new SelectListItem { Value = model.StoreId.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                            };
                        }
                    }
                }
                return View(model);
            }
            if (User.IsInRole("StoreAdmin"))
            {
                var userId = GetLoggedInUserId();
                var httpClient = GetAuthorizedHttpClient();
                var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                            model.StoreId = currentUser.StoreId.Value;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Store Admin account is not associated with a store. Cannot create items.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to retrieve Store Admin's store information. Cannot create items.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var httpClientForm = GetAuthorizedHttpClient(); // Get an authorized client for form submission
            string? imageUrl = null;

            // Upload image if provided
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    imageUrl = await _fileUploadService.UploadFileAsync(
                        model.ImageFile,
                        _configuration["FileUploadSettings:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images"),
                        null // No old image on create
                    );
                    model.ImageUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: Image upload failed: {ex.Message}");
                    ModelState.AddModelError("ImageFile", $"Failed to upload item image: {ex.Message}");
                    ViewBag.Categories = GetItemCategoriesForDropdown();
                    ViewBag.Stores = await GetStoresForDropdown();
                    if (User.IsInRole("StoreAdmin"))
                    {
                        var userId = GetLoggedInUserId();
                        var userResponse = await httpClientForm.GetAsync($"User/getsingleuser/{userId}");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true)
                            {
                                ViewBag.Stores = new List<SelectListItem> {
                                    new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                                };
                            }
                        }
                    }
                    return View(model);
                }
            }
            // We're relying on the [Required] attribute and client/server validation to ensure model.Category has a value.
            var createItemDto = new
            {
                model.Name,
                model.Description,
                model.Price,
                model.Quantity,
                ImageUrl = model.ImageUrl, // Use the uploaded image URL
                categoryType = model.categoryType.ToString(), // Convert enum to string (now guaranteed non-null if ModelState.IsValid)
                model.StoreId,
                model.IsActive
            };


            try
            {
                var response = await httpClientForm.PostAsJsonAsync("Item/CreateItem", createItemDto);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Item created successfully!";
                    return RedirectToAction(nameof(Index)); // Redirect to ItemList (now Index)
                }
                else
                {
                    ModelState.AddModelError("", $"Failed to create item: {responseContent}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while creating the item: {ex.Message}");
                Debug.WriteLine($"DEBUG: Unhandled Exception in Create (POST): {ex.Message}");
            }

            ViewBag.Categories = GetItemCategoriesForDropdown();
            ViewBag.Stores = await GetStoresForDropdown();
            if (User.IsInRole("StoreAdmin"))
            {
                var userId = GetLoggedInUserId();
                var userResponse = await httpClientForm.GetAsync($"User/getsingleuser/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        ViewBag.Stores = new List<SelectListItem> {
                            new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                        };
                    }
                }
            }
            return View(model);
        }

        // GET: /Item/Edit/{id}
        [HttpGet("/Item/Edit/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();
            ItemViewModel? item = null;
            try
            {
                var response = await httpClient.GetAsync($"Item/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    item = await response.Content.ReadFromJsonAsync<ItemViewModel>(options);
                    if (User.IsInRole("StoreAdmin") && item != null)
                    {
                        var userId = GetLoggedInUserId();
                        var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true && item.StoreId != currentUser.StoreId.Value)
                            {
                                TempData["ErrorMessage"] = "You can only edit items belonging to your assigned store.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to load item for editing: {responseContent}";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading item: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = GetItemCategoriesForDropdown();
            if (User.IsInRole("SuperAdmin"))
            {
                ViewBag.Stores = await GetStoresForDropdown();
            }
            else if (User.IsInRole("StoreAdmin") && item != null)
            {
                ViewBag.Stores = new List<SelectListItem> {
                    new SelectListItem { Value = item.StoreId.ToString(), Text = item.StoreName ?? "Assigned Store" }
                };
            }
            return View(item);
        }

        // POST: /Item/Edit/{id}
        [HttpPost("/Item/Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> Edit(Guid id, ItemViewModel model)
        {
            if (id != model.Id)
            {
                ModelState.AddModelError("", "Item ID mismatch.");
            }
            if (User.IsInRole("StoreAdmin"))
            {
                var userId = GetLoggedInUserId();
                var httpClient = GetAuthorizedHttpClient();
                var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        if (model.StoreId != currentUser.StoreId.Value)
                        {
                            model.StoreId = currentUser.StoreId.Value;
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Store Admin account is not associated with a store.");
                    }
                }
            }
            // Server-side model validation based on DataAnnotations on ItemViewModel
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = GetItemCategoriesForDropdown();
                ViewBag.Stores = await GetStoresForDropdown(); // Reload stores for SuperAdmin
                if (User.IsInRole("StoreAdmin"))
                {
                    var userId = GetLoggedInUserId();
                    var httpClientLocal = GetAuthorizedHttpClient(); // Instantiate for this scope
                    var userResponse = await httpClientLocal.GetAsync($"User/getsingleuser/{userId}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                        if (currentUser?.StoreId.HasValue == true)
                        {
                            ViewBag.Stores = new List<SelectListItem> {
                                new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                            };
                        }
                    }
                }
                return View(model);
            }

            var httpClientForm = GetAuthorizedHttpClient(); // Get an authorized client for form submission
            string? newImageUrl = model.ImageUrl; // Keep current image URL by default

            // Handle new image upload
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    newImageUrl = await _fileUploadService.UploadFileAsync(
                        model.ImageFile,
                        _configuration["FileUploadSettings:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images"),
                        model.ImageUrl // Pass old image URL for deletion
                    );
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFile", $"Failed to upload new item image: {ex.Message}");
                    ViewBag.Categories = GetItemCategoriesForDropdown();
                    ViewBag.Stores = await GetStoresForDropdown();
                    if (User.IsInRole("StoreAdmin"))
                    {
                        var userId = GetLoggedInUserId();
                        var userResponse = await httpClientForm.GetAsync($"User/getsingleuser/{userId}");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true)
                            {
                                ViewBag.Stores = new List<SelectListItem> {
                                    new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                                };
                            }
                        }
                    }
                    return View(model);
                }
            }

            // Prepare DTO for backend update
            var updateItemDto = new
            {
                model.Id, // Ensure ID is sent in the body
                model.Name,
                model.Description,
                model.Price,
                model.Quantity,
                ImageUrl = newImageUrl, // Use the new or existing image URL
                categoryType = model.categoryType.ToString(), // Convert enum to string
                model.StoreId, // Ensure the correct StoreId is sent (SuperAdmin can change, StoreAdmin is fixed)
                model.IsActive
            };


            try
            {
                var response = await httpClientForm.PutAsJsonAsync($"Item/UpdateItem/{id}", updateItemDto);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Item updated successfully!";
                    return RedirectToAction(nameof(Index)); // Redirect to ItemList (now Index)
                }
                else
                {
                    ModelState.AddModelError("", $"Failed to update item: {responseContent}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the item: {ex.Message}");
            }

            ViewBag.Categories = GetItemCategoriesForDropdown();
            ViewBag.Stores = await GetStoresForDropdown();
            if (User.IsInRole("StoreAdmin"))
            {
                var userId = GetLoggedInUserId();
                var userResponse = await httpClientForm.GetAsync($"User/getsingleuser/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        ViewBag.Stores = new List<SelectListItem> {
                            new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Assigned Store" }
                        };
                    }
                }
            }
            return View(model);
        }

        // POST: /Item/Delete/{id}
        [HttpPost("/Item/DeleteItem/{id:guid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                if (User.IsInRole("StoreAdmin"))
                {
                    var userId = GetLoggedInUserId();
                    var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var currentUser = await userResponse.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                        var itemResponse = await httpClient.GetAsync($"Item/{id}");
                        if (itemResponse.IsSuccessStatusCode)
                        {
                            var itemToDelete = await itemResponse.Content.ReadFromJsonAsync<ItemViewModel>(options);
                            if (currentUser?.StoreId.HasValue == true && itemToDelete?.StoreId != currentUser.StoreId.Value)
                            {
                                TempData["ErrorMessage"] = "You can only delete items belonging to your assigned store.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to retrieve item details for deletion check.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve Store Admin's store information for deletion check.";
                        return RedirectToAction("Login", "Auth");
                    }
                }
                var response = await httpClient.DeleteAsync($"Item/DeleteItem/{id}"); // Call backend DeleteItem API
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Item deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to delete item: {responseContent}";
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Network error while deleting item: {ex.Message}";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper to check if a store exists (for SuperAdmin assigning items)
        private async Task<bool> StoreExists(Guid storeId)
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync($"stores/storedetails/{storeId}");
                return response.IsSuccessStatusCode; // If status code is 200, store exists
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Redirects a user to the item list filtered by their assigned store
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetStoreItems(Guid? storeId)
        {
            var httpClient = GetAuthorizedHttpClient();
            var userId = GetLoggedInUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Auth");
            }
            var userResponse = await httpClient.GetAsync($"User/getsingleuser/{userId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                var currentUser = await userResponse.Content.ReadFromJsonAsync<ItemManagement.Models.ViewModels.UserEditViewModel>(options);
                if (currentUser?.StoreId.HasValue == true)
                {
                    // Fetch items for this store
                    var itemsResponse = await httpClient.GetAsync($"Item/GetAllItems?storeId={currentUser.StoreId.Value}");
                    if (itemsResponse.IsSuccessStatusCode)
                    {
                        var itemOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                        itemOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                        var items = await itemsResponse.Content.ReadFromJsonAsync<List<ItemManagement.Models.ViewModels.ItemViewModel>>(itemOptions);
                        return View("GetStoreItems", items ?? new List<ItemManagement.Models.ViewModels.ItemViewModel>());
                    }
                }
            }
            // If no store assigned or error, show empty list
            return View("GetStoreItems", new List<ItemManagement.Models.ViewModels.ItemViewModel>());
        }
    }
}