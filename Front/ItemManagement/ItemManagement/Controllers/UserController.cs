using ItemManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ItemManagement.FileUpload;
using ItemManagement.Helper;
using System.Text; // For StringContent
using System.Linq;

namespace ItemManagement.Controllers
{
    // Authorize all actions by default, then override with specific roles where needed
    [Authorize]
    [Route("[controller]/[action]")] // Ensures consistent routing like /User/Profile
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration; // Needed for FileUploadSettings:UploadDirectory
        private readonly FileUploadService _fileUploadService;
        private readonly string _imageUploadPath;
        private readonly StoreLocation _storeLocation; // Needed for store location helpers
       
        public UserController(IHttpClientFactory httpClientFactory, IConfiguration configuration,
            FileUploadService fileUploadService, StoreLocation storeLocation)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
            _imageUploadPath = configuration["FileUploadSettings:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            _storeLocation = storeLocation;
        }

        private HttpClient GetAuthorizedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient("ItemManagementApiClient");
            var accessToken = User.FindFirst("AccessToken")?.Value;

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is missing. Please log in again.");
            }
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        
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
                catch (Exception)
                {
                    // Log the exception if needed, but do not use Debug.WriteLine
                }
            }
            return loggedInUserId;
        }

      
        public IActionResult Dashboard()
        {
            // If the user is a regular User, redirect to their store's item list
            if (User.IsInRole("User"))
            {
                return RedirectToAction("GetStoreItems", "Item");
            }
            return View();
        }

        #region User Management (Super Admin & Store Admin)

      
        [HttpGet]
        public async Task<IActionResult> UserList()
        {
            var authorizedClient = GetAuthorizedHttpClient();
            List<UserViewModel> users = new List<UserViewModel>();
            string requestUri = "User/GetAllUser"; // Reverted to match backend route

            Guid loggedInUserId = GetLoggedInUserId();
            Guid? storeIdToFilter = null;

            // If the logged-in user is a StoreAdmin, get their store ID to filter users
            if (User.IsInRole("StoreAdmin"))
            {
                if (loggedInUserId != Guid.Empty)
                {
                    try
                    {
                        // Use /api/ prefix only for this call
                        var userDetailsResponse = await authorizedClient.GetAsync($"User/getsingleuser/{loggedInUserId}");
                        if (userDetailsResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUserDetails = await userDetailsResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                            if (currentUserDetails?.StoreId.HasValue == true)
                            {
                                storeIdToFilter = currentUserDetails.StoreId.Value;
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Store Admin account is not associated with a store. Cannot display user list.";
                                return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to retrieve Store Admin's details to filter users.";
                            return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                        return RedirectToAction("Login", "Auth");
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"An error occurred while retrieving your store information: {ex.Message}";
                        return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not identify logged-in Store Admin. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }
            }

            try
            {
                var response = await authorizedClient.GetAsync($"{requestUri}?storeId={storeIdToFilter}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>(options) ?? new List<UserViewModel>();
                    System.Diagnostics.Debug.WriteLine($"Fetched {users.Count} users from backend.");
                    foreach (var u in users)
                    {
                        System.Diagnostics.Debug.WriteLine($"User: {u.FName} {u.LName}, StoreId: {u.StoreId}, Role: {u.Role}");
                    }

                    // Filter out the currently logged-in user from the list
                    if (loggedInUserId != Guid.Empty && users != null)
                    {
                        users = users.Where(u => u.Id != loggedInUserId).ToList();
                    }

                    // For StoreAdmin, filter users by their store
                    if (User.IsInRole("StoreAdmin") && storeIdToFilter.HasValue)
                    {
                        users = users.Where(u => u.StoreId == storeIdToFilter).ToList();
                        System.Diagnostics.Debug.WriteLine($"After filtering: {users.Count} users for store {storeIdToFilter}");
                    }

                    // Fetch all stores to get store names for display
                    var storesResponse = await authorizedClient.GetAsync("stores/allstores");
                    if (storesResponse.IsSuccessStatusCode)
                    {
                        var stores = await storesResponse.Content.ReadFromJsonAsync<List<StoreViewModel>>(options);
                        var storeDictionary = stores?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<Guid, string>();

                        // Populate store names for each user in the displayed list
                        foreach (var user in users)
                        {
                            if (user.StoreId.HasValue && storeDictionary.ContainsKey(user.StoreId.Value))
                            {
                                user.StoreName = storeDictionary[user.StoreId.Value];
                            }
                        }
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Could not fetch store names. Store information may be incomplete.";
                    }

                    // Sort users by CreateDate descending (latest first)
                    users = users.OrderByDescending(u => u.CreateDate).ToList();

                    return View("~/Views/User/UserList.cshtml", users);
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to retrieve user list. Status code: {response.StatusCode}";
                    return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Network error while retrieving user list: {ex.Message}";
                return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                return View("~/Views/User/UserList.cshtml", new List<UserViewModel>());
            }
        }

      
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> CreateUser()
        {
            var model = new UserEditViewModel();

            // Set up available roles based on the current user's role
            if (User.IsInRole("SuperAdmin"))
            {
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "SuperAdmin", Text = "Super Admin" },
                    new SelectListItem { Value = "StoreAdmin", Text = "Store Admin" },
                    new SelectListItem { Value = "User", Text = "User" }
                };
                model.AvailableStores = await GetStoresForDropdown();
            }
            else // StoreAdmin
            {
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "User", Text = "User" }
                };

                // For StoreAdmin, automatically set their store
                var loggedInUserId = GetLoggedInUserId();
                var authorizedClient = GetAuthorizedHttpClient();
                var userResponse = await authorizedClient.GetAsync($"User/getsingleuser/{loggedInUserId}");
                
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        model.StoreId = currentUser.StoreId.Value;
                        model.AvailableStores = new List<SelectListItem> {
                        new SelectListItem { Value = currentUser.StoreId.Value.ToString(), Text = currentUser.StoreName ?? "Your Store", Selected = true }
                        };
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Store Admin account is not associated with a store. Cannot create users.";
                        return RedirectToAction(nameof(UserList));
                    }
                }
            }

            return View("~/Views/User/CreateUser.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> CreateUser(UserEditViewModel model)
        {
            // For StoreAdmin, ensure they can only create users for their store
            if (User.IsInRole("StoreAdmin"))
            {
                var loggedInUserId = GetLoggedInUserId();
                var authorizedClient = GetAuthorizedHttpClient();
                var userResponse = await authorizedClient.GetAsync($"User/getsingleuser/{loggedInUserId}");
                
                if (userResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var currentUser = await userResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                    if (currentUser?.StoreId.HasValue == true)
                    {
                        // Force the store ID to be the admin's store
                        model.StoreId = currentUser.StoreId.Value;
                        // Force role to be User
                        model.Role = "User";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Store Admin account is not associated with a store. Cannot create users.";
                        return RedirectToAction(nameof(UserList));
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload dropdowns if model state is invalid
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                    new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                    new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                };
                model.AvailableStores = await GetStoresForDropdown();
                return View("~/Views/User/CreateUser.cshtml", model);
            }

            var httpClient = GetAuthorizedHttpClient();
            string? profileImageUrl = null;

            if (model.NewProfileImage != null && model.NewProfileImage.Length > 0)
            {
                try
                {
                    profileImageUrl = await _fileUploadService.UploadFileAsync(
                        model.NewProfileImage,
                        _imageUploadPath,
                        model.CurrentProfileImageUrl // Pass old image URL for deletion (if available)
                    );
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewProfileImage", $"Failed to upload profile picture: {ex.Message}");
                    // Reload dropdowns on error
                    model.AvailableRoles = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                        new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                        new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                    };
                    model.AvailableStores = await GetStoresForDropdown();
                    return View("~/Views/User/CreateUser.cshtml", model);
                }
            }

            var createData = new
            {
                model.FName,
                model.LName,
                model.Email,
                model.Password, // Ensure password is handled securely (e.g., hashed in backend API)
                model.MobileNumber,
                Profile = profileImageUrl,
                model.Role,
                model.IsActive,
                model.StoreId
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("User/CreateUser", createData);

            if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction(nameof(UserList));
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("Email", $"Email already in use. {errorContent}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to create user: {response.StatusCode}. Error: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Network error: {ex.Message}. Could not create user.");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
            }

            // If we reach here, something failed, reload the view with model errors
            model.AvailableRoles = new List<SelectListItem>
            {
                new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
            };
            model.AvailableStores = await GetStoresForDropdown();
            return View("~/Views/User/CreateUser.cshtml", model);
        }

        /// <summary>
        /// Displays the form to edit an existing user. Accessible by Super Admins and Store Admins (for their store's users).
        /// </summary>
        /// <param name="id">The GUID of the user to edit.</param>
        /// <returns>A view with the edit user form, or redirects on error/unauthorized access.</returns>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> EditUser(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var response = await httpClient.GetAsync($"User/getsingleuser/{id}");

            if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserEditViewModel>(options);
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User not found for editing.";
                        return RedirectToAction(nameof(UserList));
                    }

                    // Authorization check for StoreAdmin: ensure they can only edit users from their assigned store
                    if (User.IsInRole("StoreAdmin"))
                    {
                        var loggedInUserId = GetLoggedInUserId();
                        var currentUserDetailsResponse = await httpClient.GetAsync($"User/getsingleuser/{loggedInUserId}");
                        if (currentUserDetailsResponse.IsSuccessStatusCode)
                        {
                            var currentUserDetails = await currentUserDetailsResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                            if (currentUserDetails?.StoreId.HasValue == true && user.StoreId != currentUserDetails.StoreId.Value)
                            {
                                TempData["ErrorMessage"] = "You can only edit users from your assigned store.";
                                return RedirectToAction(nameof(UserList));
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to verify your store association for editing.";
                            return RedirectToAction(nameof(UserList));
                        }
                    }

                    user.AvailableRoles = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = user.Role == "SuperAdmin" },
                        new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = user.Role == "StoreAdmin" },
                        new SelectListItem { Value = "User", Text = "User", Selected = user.Role == "User" }
                    };
                    user.AvailableStores = await GetStoresForDropdown();

                    return View("~/Views/User/EditUser.cshtml", user);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(UserList));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to load user for editing: {response.StatusCode}. Error: {errorContent}";
                    return RedirectToAction(nameof(UserList));
                }
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "Network error. Could not connect to the user service.";
                return RedirectToAction(nameof(UserList));
            }
            catch (JsonException)
            {
                TempData["ErrorMessage"] = "Error processing user data from the server for editing.";
                return RedirectToAction(nameof(UserList));
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                return RedirectToAction(nameof(UserList));
            }
        }

        [HttpPost("{id:guid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> EditUser(Guid id, UserEditViewModel model)
        {
            if (id != model.Id)
            {
                ModelState.AddModelError("", "User ID mismatch.");
                model.AvailableRoles = new List<SelectListItem> // Reload dropdowns
                {
                    new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                    new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                    new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                };
                model.AvailableStores = await GetStoresForDropdown();
                return View("~/Views/User/EditUser.cshtml", model);
            }

            // Authorization check for StoreAdmin (for POST): prevent editing users outside their store or changing store ID
            if (User.IsInRole("StoreAdmin"))
            {
                var loggedInUserId = GetLoggedInUserId();
                var httpClientLocal = GetAuthorizedHttpClient();
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                var currentUserDetailsResponse = await httpClientLocal.GetAsync($"User/getsingleuser/{loggedInUserId}");
                if (!currentUserDetailsResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to verify your store association for updating.";
                    return RedirectToAction(nameof(UserList));
                }
                var currentUserDetails = await currentUserDetailsResponse.Content.ReadFromJsonAsync<UserViewModel>(options);

                var userBeingEditedResponse = await httpClientLocal.GetAsync($"User/getsingleuser/{id}");
                if (!userBeingEditedResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to retrieve details of user being edited.";
                    return RedirectToAction(nameof(UserList));
                }
                var userBeingEdited = await userBeingEditedResponse.Content.ReadFromJsonAsync<UserViewModel>(options);

                if (currentUserDetails?.StoreId.HasValue == true &&
                    (userBeingEdited?.StoreId != currentUserDetails.StoreId.Value || model.StoreId != currentUserDetails.StoreId.Value))
                {
                    TempData["ErrorMessage"] = "You can only update users from your assigned store and cannot change their assigned store.";
                    model.AvailableRoles = new List<SelectListItem> // Reload dropdowns for view
                    {
                        new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                        new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                        new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                    };
                    model.AvailableStores = await GetStoresForDropdown();
                    return View("~/Views/User/EditUser.cshtml", model);
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the form errors.";
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                    new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                    new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                };
                model.AvailableStores = await GetStoresForDropdown();
                return View("~/Views/User/EditUser.cshtml", model);
            }

            var httpClient = GetAuthorizedHttpClient();
            string? newProfileImageUrl = model.CurrentProfileImageUrl;

            if (model.NewProfileImage != null && model.NewProfileImage.Length > 0)
            {
                try
                {
                    newProfileImageUrl = await _fileUploadService.UploadFileAsync(
                        model.NewProfileImage,
                        _imageUploadPath,
                        model.CurrentProfileImageUrl // Pass old image URL for deletion (if available)
                    );
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewProfileImage", $"Failed to upload new profile picture: {ex.Message}");
                    model.AvailableRoles = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                        new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                        new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
                    };
                    model.AvailableStores = await GetStoresForDropdown();
                    return View("~/Views/User/EditUser.cshtml", model);
                }
            }

            var updateData = new
            {
                model.Id,
                model.FName,
                model.LName,
                model.Email,
                model.MobileNumber,
                Profile = newProfileImageUrl,
                model.Role,
                model.IsActive,
                model.StoreId
            };

            try
            {
                var response = await httpClient.PutAsJsonAsync($"User/UpdateUser/{id}", updateData);
                var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(UserList));
                }
                else
                {
                    ModelState.AddModelError("", $"Failed to update user: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Network error: {ex.Message}. Could not update user.");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
            }

            // If we reach here, something failed, reload the view with model errors
            model.AvailableRoles = new List<SelectListItem>
            {
                new SelectListItem { Value = "SuperAdmin", Text = "Super Admin", Selected = model.Role == "SuperAdmin" },
                new SelectListItem { Value = "StoreAdmin", Text = "Store Admin", Selected = model.Role == "StoreAdmin" },
                new SelectListItem { Value = "User", Text = "User", Selected = model.Role == "User" }
            };
            model.AvailableStores = await GetStoresForDropdown();
            return View("~/Views/User/EditUser.cshtml", model);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var authorizedClient = GetAuthorizedHttpClient();

            try
            {
                // For StoreAdmin, verify the user belongs to their store
                if (User.IsInRole("StoreAdmin"))
                {
                    var loggedInUserId = GetLoggedInUserId();
                    var adminResponse = await authorizedClient.GetAsync($"User/getsingleuser/{loggedInUserId}");
                    var userResponse = await authorizedClient.GetAsync($"User/getsingleuser/{id}");
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                    if (adminResponse.IsSuccessStatusCode && userResponse.IsSuccessStatusCode)
                    {
                        var admin = await adminResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                        var userToDelete = await userResponse.Content.ReadFromJsonAsync<UserViewModel>(options);

                        if (admin?.StoreId != userToDelete?.StoreId)
                        {
                            TempData["ErrorMessage"] = "You can only delete users from your own store.";
                            return RedirectToAction(nameof(UserList));
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to verify user store association.";
                        return RedirectToAction(nameof(UserList));
                    }
                }

                var response = await authorizedClient.DeleteAsync($"User/DeleteUser/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to delete user: {errorContent}";
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Network error: {ex.Message}. Could not delete user.";
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

            return RedirectToAction(nameof(UserList));
        }

        #endregion

        #region User Profile Management (All Users)

        /// <summary>
        /// Displays the current user's profile for viewing and editing. Accessible to all authenticated users.
        /// </summary>
        /// <returns>A view with the user's profile details.</returns>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var userId = GetLoggedInUserId(); // Use the common helper method

                if (userId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "User ID not found. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }

                var url = $"User/getsingleuser/{userId}"; // Assuming backend API is getsingleuser
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserViewModel>(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User profile not found.";
                        return RedirectToAction("Login", "Auth"); // Or a generic error page
                    }

                    var profileViewModel = new UserProfileUpdateViewModel
                    {
                        Id = user.Id,
                        FName = user.FName,
                        LName = user.LName,
                        Email = user.Email,
                        MobileNumber = user.MobileNumber,
                        CurrentProfilePicture = user.Profile
                    };
                    return View(profileViewModel);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Unable to load profile: {response.StatusCode} - {errorContent}";
                return View(new UserProfileUpdateViewModel()); // Return empty model on error
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading your profile: {ex.Message}";
                return View(new UserProfileUpdateViewModel());
            }
        }

        /// <summary>
        /// Handles the submission for updating the current user's profile. Accessible to all authenticated users.
        /// </summary>
        /// <param name="model">The UserProfileUpdateViewModel with updated profile data.</param>
        /// <returns>Redirects to Profile on success, or returns the view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileUpdateViewModel model)
        {
            // Ensure the ID in the model matches the logged-in user's ID
            var loggedInUserId = GetLoggedInUserId();
            if (loggedInUserId == Guid.Empty || model.Id != loggedInUserId)
            {
                TempData["ErrorMessage"] = "Unauthorized profile update attempt.";
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            try
            {
                var httpClient = GetAuthorizedHttpClient();
                string imagePath = model.CurrentProfilePicture; // Default to current picture

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    // Upload new image if provided
                    imagePath = await _fileUploadService.UploadFileAsync(model.ProfileImage, _imageUploadPath);
                }

                var updateData = new
                {
                    model.Id,
                    model.FName,
                    model.LName,
                    model.Email,
                    model.MobileNumber,
                    Profile = imagePath,
                    model.CurrentPassword, // Sent to API for verification
                    model.NewPassword      // New password if changing
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                var url = "User/updateprofile"; // Assuming backend API is updateprofile
                var response = await httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }

                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to update profile: {error}");
                return View("Profile", model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating your profile: {ex.Message}");
                return View("Profile", model);
            }
        }

        //#endregion

        //#region Store Management (Super Admin)

        /// <summary>
        /// Displays a list of all stores. Accessible by Super Admins.
        /// </summary>
        /// <returns>A view with a list of stores.</returns>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> StoreList()
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync("stores/allstores");
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    var stores = await response.Content.ReadFromJsonAsync<List<StoreViewModel>>(options);

                    if (stores != null)
                    {
                        foreach (var store in stores)
                        {
                            store.Country = await _storeLocation.GetCountryNameAsync(store.CountryId);
                            store.State = await _storeLocation.GetStateNameAsync(store.StateId);
                            store.City = await _storeLocation.GetCityNameAsync(store.CityId);
                        }
                    }

                    ViewBag.Stores = stores ?? new List<StoreViewModel>();
                    return View("~/Views/Store/StoreList.cshtml");
                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to load stores. Please try again later.";
                    ViewBag.Stores = new List<StoreViewModel>();
                    return View("~/Views/Store/StoreList.cshtml");
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewBag.Stores = new List<StoreViewModel>();
                return View("~/Views/Store/StoreList.cshtml");
            }
        }

        /// <summary>
        /// Displays the form to create a new store. Accessible by Super Admins.
        /// </summary>
        /// <returns>A view with the create store form.</returns>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateStore()
        {
            var model = new StoreViewModel();
            var countries = await _storeLocation.GetAllCountriesAsync();
            var states = await _storeLocation.GetStatesByCountryAsync(model.CountryId);
            var cities = await _storeLocation.GetCitiesByStateAsync(model.StateId);
            ViewBag.Countries = new SelectList(countries, "Id", "Name", model.CountryId);
            ViewBag.States = new SelectList(states, "Id", "Name", model.StateId);
            ViewBag.Cities = new SelectList(cities, "Id", "Name", model.CityId);
            return View("~/Views/Store/CreateStore.cshtml", model);
        }

        /// <summary>
        /// Handles the submission for creating a new store. Accessible by Super Admins.
        /// </summary>
        /// <param name="model">The StoreViewModel containing new store data.</param>
        /// <returns>Redirects to StoreList on success, or returns the view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateStore(StoreViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Store/CreateStore.cshtml", model);
            }

            var httpClient = GetAuthorizedHttpClient();
            string? imageUrl = null;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    imageUrl = await _fileUploadService.UploadFileAsync(model.ImageFile, _imageUploadPath);
                    model.ImageUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFile", $"Failed to upload store image: {ex.Message}");
                    return View("~/Views/Store/CreateStore.cshtml", model);
                }
            }

            try
            {
                var storeData = StoreViewModel.ToStoreDto(model);
                var response = await httpClient.PostAsJsonAsync("stores/addstore", storeData);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Store created successfully!";
                    return RedirectToAction(nameof(StoreList));
                }
                else
                {
                    ModelState.AddModelError("", $"Failed to create store: {responseContent}");
                    return View("~/Views/Store/CreateStore.cshtml", model);
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while creating the store: {ex.Message}");
                return View("~/Views/Store/CreateStore.cshtml", model);
            }
        }

        /// <summary>
        /// Displays the form to edit an existing store. Accessible by Super Admins.
        /// </summary>
        /// <param name="id">The GUID of the store to edit.</param>
        /// <returns>A view with the edit store form, or redirects on error.</returns>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditStore(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync($"stores/storedetails/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var store = await response.Content.ReadFromJsonAsync<StoreViewModel>(options);

                    if (store == null)
                    {
                        TempData["ErrorMessage"] = "Store not found.";
                        return RedirectToAction(nameof(StoreList));
                    }

                    // Ensure properties are not null before assigning to view model
                    store.Name = store.Name ?? string.Empty;
                    store.Email = store.Email ?? string.Empty;
                    store.ContactNumber = store.ContactNumber ?? string.Empty;
                    store.Address = store.Address ?? string.Empty;
                    store.ImageUrl = store.ImageUrl ?? string.Empty;

                    // Load dropdowns for location
                    var countries = await _storeLocation.GetAllCountriesAsync();
                    ViewBag.Countries = new SelectList(countries, "Id", "Name", store.CountryId);

                    var states = await _storeLocation.GetStatesByCountryAsync(store.CountryId);
                    ViewBag.States = new SelectList(states, "Id", "Name", store.StateId);

                    var cities = await _storeLocation.GetCitiesByStateAsync(store.StateId);
                    ViewBag.Cities = new SelectList(cities, "Id", "Name", store.CityId);

                    return View("~/Views/Store/EditStore.cshtml", store);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to load store for editing.";
                    return RedirectToAction(nameof(StoreList));
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading the store: {ex.Message}";
                return RedirectToAction(nameof(StoreList));
            }
        }

        /// <summary>
        /// Handles the submission for editing an existing store. Accessible by Super Admins.
        /// </summary>
        /// <param name="id">The GUID of the store being edited.</param>
        /// <param name="model">The StoreViewModel containing updated store data.</param>
        /// <returns>Redirects to StoreList on success, or returns the view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditStore(Guid id, StoreViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the form errors.";
                // Reload dropdowns on validation failure
                ViewBag.Countries = new SelectList(await _storeLocation.GetAllCountriesAsync(), "Id", "Name", model.CountryId);
                ViewBag.States = new SelectList(await _storeLocation.GetStatesByCountryAsync(model.CountryId), "Id", "Name", model.StateId);
                ViewBag.Cities = new SelectList(await _storeLocation.GetCitiesByStateAsync(model.StateId), "Id", "Name", model.CityId);
                return View("~/Views/Store/EditStore.cshtml", model);
            }

            var httpClient = GetAuthorizedHttpClient();
            try
            {
                // Handle image upload if a new file is provided
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var newImageUrl = await _fileUploadService.UploadFileAsync(model.ImageFile, _imageUploadPath);
                    if (!string.IsNullOrEmpty(newImageUrl))
                    {
                        model.ImageUrl = newImageUrl;
                    }
                }

                var storeData = StoreViewModel.ToStoreDto(model);
                var response = await httpClient.PutAsJsonAsync($"stores/updatestore/{id}", storeData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Store updated successfully!";
                    return RedirectToAction(nameof(StoreList));
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to update store: {response.StatusCode} - {responseContent}";
                    ModelState.AddModelError("", $"Failed to update store: {response.StatusCode} - {responseContent}");
                    // Reload dropdowns on API failure
                    ViewBag.Countries = new SelectList(await _storeLocation.GetAllCountriesAsync(), "Id", "Name", model.CountryId);
                    ViewBag.States = new SelectList(await _storeLocation.GetStatesByCountryAsync(model.CountryId), "Id", "Name", model.StateId);
                    ViewBag.Cities = new SelectList(await _storeLocation.GetCitiesByStateAsync(model.StateId), "Id", "Name", model.CityId);
                    return View("~/Views/Store/EditStore.cshtml", model);
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the store: {ex.Message}");
                // Reload dropdowns on exception
                ViewBag.Countries = new SelectList(await _storeLocation.GetAllCountriesAsync(), "Id", "Name", model.CountryId);
                ViewBag.States = new SelectList(await _storeLocation.GetStatesByCountryAsync(model.CountryId), "Id", "Name", model.StateId);
                ViewBag.Cities = new SelectList(await _storeLocation.GetCitiesByStateAsync(model.StateId), "Id", "Name", model.CityId);
                return View("~/Views/Store/EditStore.cshtml", model);
            }
        }

        /// <summary>
        /// Handles the deletion of a store. Also disassociates users from the deleted store. Accessible by Super Admins.
        /// </summary>
        /// <param name="id">The GUID of the store to delete.</param>
        /// <returns>Redirects to StoreList.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteStore(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();

            try
            {
                // First, get all users associated with this store and disassociate them
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var usersResponse = await httpClient.GetAsync("User/GetAllUser");

                if (usersResponse.IsSuccessStatusCode)
                {
                    var users = await usersResponse.Content.ReadFromJsonAsync<List<UserViewModel>>(options) ?? new List<UserViewModel>();
                    var storeUsers = users.Where(u => u.StoreId == id).ToList();

                    foreach (var user in storeUsers)
                    {
                        var updateData = new
                        {
                            user.Id,
                            user.FName,
                            user.LName,
                            user.Email,
                            user.MobileNumber,
                            Profile = user.Profile,
                            user.Role,
                            user.IsActive,
                            StoreId = (Guid?)null // Disassociate by setting StoreId to null
                        };

                        var updateResponse = await httpClient.PutAsJsonAsync($"User/UpdateUser/{user.Id}", updateData);
                        if (!updateResponse.IsSuccessStatusCode)
                        {
                            TempData["ErrorMessage"] = $"Failed to remove store association from user {user.Email}. Store not deleted.";
                            return RedirectToAction(nameof(StoreList));
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch users to disassociate from store. Store not deleted.";
                    return RedirectToAction(nameof(StoreList));
                }

                // Now, delete the store itself
                var response = await httpClient.DeleteAsync($"stores/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Store deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to delete store. Status: {response.StatusCode}, Error: {responseContent}";
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ErrorMessage"] = $"Network error while deleting store: {ex.Message}";
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
            return RedirectToAction(nameof(StoreList));
        }

        /// <summary>
        /// Displays the details of a specific store. Accessible by Super Admins.
        /// </summary>
        /// <param name="id">The GUID of the store to display.</param>
        /// <returns>A view with the store details, or redirects on error.</returns>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> StoreDetails(Guid id)
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync($"stores/storedetails/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var store = await response.Content.ReadFromJsonAsync<StoreViewModel>(options);

                    if (store == null)
                    {
                        TempData["ErrorMessage"] = "Store not found.";
                        return RedirectToAction(nameof(StoreList));
                    }

                    // Populate location names
                    store.Country = await _storeLocation.GetCountryNameAsync(store.CountryId);
                    store.State = await _storeLocation.GetStateNameAsync(store.StateId);
                    store.City = await _storeLocation.GetCityNameAsync(store.CityId);

                    return View("~/Views/Store/StoreDetails.cshtml", store);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to load store details.";
                    return RedirectToAction(nameof(StoreList));
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Authentication required. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading store details: {ex.Message}";
                return RedirectToAction(nameof(StoreList));
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Retrieves a list of stores for use in dropdowns (e.g., when assigning a user to a store).
        /// </summary>
        /// <returns>A list of SelectListItem containing store IDs and names.</returns>
        private async Task<List<SelectListItem>> GetStoresForDropdown()
        {
            var httpClient = GetAuthorizedHttpClient();
            try
            {
                var response = await httpClient.GetAsync("stores/allstores");
                response.EnsureSuccessStatusCode(); // Throws on HTTP error codes
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var stores = await response.Content.ReadFromJsonAsync<List<StoreViewModel>>(options);

                return stores?.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList() ?? new List<SelectListItem>();
            }
            catch (Exception)
            {
                // In a real application, consider logging this error or providing a more specific message.
                // For dropdowns, returning an empty list is generally acceptable as it will just show no options.
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Provides SelectListItems for item categories (Veg, Non-Veg, Egg).
        /// </summary>
        /// <returns>A list of SelectListItem for item categories.</returns>
        private List<SelectListItem> GetItemCategoriesForDropdown()
        {
            // Assuming Category is an enum like:
            // public enum Category { Veg, NonVeg, Egg }
            return Enum.GetValues(typeof(Category))
                       .Cast<Category>()
                       .Select(c => new SelectListItem
                       {
                           Value = c.ToString(), // "Veg", "NonVeg", "Egg"
                           Text = c.ToString()   // Display text
                       })
                       .ToList();
        }

        #endregion


    }
}
