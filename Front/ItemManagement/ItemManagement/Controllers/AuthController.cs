using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using ItemManagement.Models.ViewModels;
using ItemManagement.FileUpload;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Diagnostics; // REQUIRED for Debug.WriteLine
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;   

namespace ItemManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FileUploadService _fileUploadService;
        private readonly string _imageUploadPath;

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, FileUploadService fileUploadService)
        {
            _httpClientFactory = httpClientFactory;
            _fileUploadService = fileUploadService;
            _imageUploadPath = configuration["FileUploadSettings:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //string? imagePath = null;

            //if (model.Profile != null && model.Profile.Length > 0)
            //{
            //    try
            //    {
            //        imagePath = await _fileUploadService.UploadFileAsync(model.Profile, _imageUploadPath);
            //    }
            //    catch (Exception ex)
            //    {
            //        ModelState.AddModelError("", $"Failed to upload profile picture: {ex.Message}");
            //        return View(model);
            //    }
            //}

            var registrationData = new
            {
                model.FName,
                model.LName,
                model.Email,
                model.MobileNumber,
                model.Role
                // Password and Profile removed from registration data
            };

            var httpClient = _httpClientFactory.CreateClient("ItemManagementApiClient");
          
            try
            {
                var response = await httpClient.PostAsJsonAsync("Auth/register", registrationData);


                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Registration successful. Please check your email to set your password.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    Dictionary<string, string>? errorObject = null;
                    try
                    {
                        errorObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        ModelState.AddModelError("", $"Error parsing backend response: {ex.Message}. The backend might not be returning valid JSON for success or error responses.");
                    }
                    string errorMessage = errorObject != null && errorObject.ContainsKey("Message")
                                          ? errorObject["Message"]
                                          : $"Registration failed with status code {response.StatusCode}. Raw response: {errorContent}";
                    ModelState.AddModelError("", errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Network error during registration: {ex.Message}. Check if backend is running at '{httpClient.BaseAddress}' and accessible.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An unexpected error occurred during registration: {ex.Message}");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var loginData = new
            {
                model.Email,
                model.Password
            };

            var httpClient = _httpClientFactory.CreateClient("ItemManagementApiClient");

            try
            {
                var response = await httpClient.PostAsJsonAsync("Auth/login", loginData);


                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>();

                    if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Token))
                    {
                        ModelState.AddModelError("", "Login failed: Missing token in response.");
                        return View(model);
                    }


                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(apiResponse.Token);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "User"),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim("AccessToken", apiResponse.Token),
                        new Claim(ClaimTypes.Role, jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")?.Value ?? "User")
                    };

                    var storeIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "StoreId")?.Value;
                    if (!string.IsNullOrEmpty(storeIdClaim))
                    {
                        claims.Add(new Claim("StoreId", storeIdClaim));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                 new ClaimsPrincipal(claimsIdentity),
                                                 authProperties);

                    HttpContext.Session.SetString("UserSession", apiResponse.Token);

                    string role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";
                    if (role == "SuperAdmin")
                    {
                        return RedirectToAction("Dashboard", "UserDashboard", new { area = "Admin" });
                    }
                    else if (role == "StoreAdmin")
                    {
                        return RedirectToAction("Dashboard", "UserDashboard", new { area = "Store" });
                    }
                    return RedirectToAction("Dashboard", "UserDashboard");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    string errorMessage = "Login failed. Please check your email and password.";
                    try
                    {
                        var errorObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                        if (errorObject != null)
                        {
                            // Try both 'message' and 'Message' keys for flexibility
                            if (errorObject.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                                errorMessage = msg;
                            else if (errorObject.TryGetValue("Message", out var msg2) && !string.IsNullOrWhiteSpace(msg2))
                                errorMessage = msg2;
                        }
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        // Ignore, fallback to generic error message
                    }

                    ModelState.AddModelError("", errorMessage);
                }
            }
          
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An unexpected error occurred during login: {ex.Message}");
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var accessToken = User.FindFirst("AccessToken")?.Value;

            if (string.IsNullOrEmpty(accessToken))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("UserSession");
                return RedirectToAction("Login", "Auth");
            }

            var httpClient = _httpClientFactory.CreateClient("ItemManagementApiClient");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var logoutUrl = "Auth/logout";
                await httpClient.PostAsync(logoutUrl, null);
            }
            catch
            {
                // Ignore any errors during logout
            }
            finally
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("UserSession");
            }

            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SetPassword(Guid userId, string token)
        {
            var model = new SetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Call backend API
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7060/"); // Updated to your backend base URL
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    userId = model.UserId,
                    token = model.Token,
                    newPassword = model.NewPassword
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Auth/SetPassword", content);
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "Password set successfully. You can now log in.";
                    return View("SetPasswordSuccess");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, "Failed to set password: " + error);
                    return View(model);
                }
            }
        }
    }
}
