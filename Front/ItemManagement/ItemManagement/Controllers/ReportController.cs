using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using ItemManagement.Models.ViewModels;
using System.Text.Json;

namespace ItemManagement.Controllers
{
    [Authorize(Roles = "SuperAdmin,StoreAdmin")]
    public class ReportController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl = "https://localhost:7060/api/"; // Ensure this is your backend API base URL
        private readonly ILogger<ReportController> _logger;

        public ReportController(IHttpClientFactory httpClientFactory, ILogger<ReportController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
            var accessToken = HttpContext.Session.GetString("UserSession"); // Use HttpContext.Session for consistency
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
                    _logger.LogError(ex, "Error parsing AccessToken as JWT in GetLoggedInUserId method.");
                }
            }
            return loggedInUserId;
        }

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
                _logger.LogError(ex, "Error fetching stores for dropdown in ReportController.");
                return new List<SelectListItem>();
            }
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("SuperAdmin"))
            {
                var stores = await GetStoresForDropdown();
                stores.Insert(0, new SelectListItem { Value = "", Text = "All Stores (Super Admin)" });
                ViewBag.Stores = stores;
            }
            return View();
        }

        // MODIFIED: Added startDate and endDate parameters
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadReport(DateTime startDate, DateTime endDate, [FromForm] Guid? storeIdFilter)
        {
            if (startDate > endDate)
            {
                TempData["Error"] = "Start Date cannot be after End Date.";
                return RedirectToAction("Index");
            }

            var token = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var startDateString = startDate.ToString("yyyy-MM-dd");
            var endDateString = endDate.ToString("yyyy-MM-dd");
            Guid? storeIdToSend = null;

            if (User.IsInRole("StoreAdmin"))
            {
                var loggedInUserId = GetLoggedInUserId();
                if (loggedInUserId != Guid.Empty)
                {
                    try
                    {
                        var httpClientForUser = GetAuthorizedHttpClient();
                        var userDetailsResponse = await httpClientForUser.GetAsync($"User/getsingleuser/{loggedInUserId}");
                        if (userDetailsResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUserDetails = await userDetailsResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                            if (currentUserDetails?.StoreId.HasValue == true)
                            {
                                storeIdToSend = currentUserDetails.StoreId.Value;
                                _logger.LogInformation($"StoreAdmin {loggedInUserId} requesting report for their store: {storeIdToSend}");
                            }
                            else
                            {
                                TempData["Error"] = "Your Store Admin account is not associated with a store. Cannot generate report.";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            var errorContent = await userDetailsResponse.Content.ReadAsStringAsync();
                            TempData["Error"] = $"Failed to retrieve your store information: {errorContent}";
                            _logger.LogError($"Failed to get Store Admin details for report: {userDetailsResponse.StatusCode} - {errorContent}");
                            return RedirectToAction("Index");
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        TempData["Error"] = "Authentication required to get your store details. Please log in again.";
                        return RedirectToAction("Login", "Auth");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"An error occurred while fetching your store details: {ex.Message}";
                        _logger.LogError(ex, "Error fetching Store Admin's store details for report.");
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["Error"] = "Could not identify logged-in Store Admin. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                if (storeIdFilter.HasValue && storeIdFilter.Value != Guid.Empty)
                {
                    storeIdToSend = storeIdFilter.Value;
                    _logger.LogInformation($"SuperAdmin requesting report for selected store: {storeIdToSend}");
                }
                else
                {
                    storeIdToSend = null;
                    _logger.LogInformation("SuperAdmin requesting report for all stores.");
                }
            }

            // Construct the URL with startDate, endDate, and optional storeId
            var url = $"{_baseUrl}Report/download-daily-report?startDate={startDateString}&endDate={endDateString}"; // MODIFIED URL
            if (storeIdToSend.HasValue)
            {
                url += $"&storeId={storeIdToSend.Value}";
            }

            _logger.LogInformation($"Requesting report from URL: {url}");

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var fileContents = await response.Content.ReadAsByteArrayAsync();
                    var fileName = $"Sales_Report_{startDateString}_to_{endDateString}";
                    if (storeIdToSend.HasValue)
                    {
                        // To get the actual store name for the filename, you'd need to fetch it here or include it in the report data.
                        // For now, we'll use a portion of the ID as a placeholder or you can remove this part.
                        fileName += $"_Store_{storeIdToSend.Value.ToString().Substring(0, 8)}";
                    }
                    fileName += ".xlsx";

                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["Error"] = $"Failed to generate report. The server responded with: {response.ReasonPhrase} - {errorContent}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report.");
                TempData["Error"] = "A network error occurred while trying to download the report.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadIndividualOrderReport(DateTime startDate, DateTime endDate, [FromForm] Guid? storeIdFilter)
        {
            if (startDate > endDate)
            {
                TempData["Error"] = "Start Date cannot be after End Date.";
                return RedirectToAction("Index");
            }

            var token = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var startDateString = startDate.ToString("yyyy-MM-dd");
            var endDateString = endDate.ToString("yyyy-MM-dd");
            Guid? storeIdToSend = null;

            if (User.IsInRole("StoreAdmin"))
            {
                var loggedInUserId = GetLoggedInUserId();
                if (loggedInUserId != Guid.Empty)
                {
                    try
                    {
                        var httpClientForUser = GetAuthorizedHttpClient();
                        var userDetailsResponse = await httpClientForUser.GetAsync($"User/getsingleuser/{loggedInUserId}");
                        if (userDetailsResponse.IsSuccessStatusCode)
                        {
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var currentUserDetails = await userDetailsResponse.Content.ReadFromJsonAsync<UserViewModel>(options);
                            if (currentUserDetails?.StoreId.HasValue == true)
                            {
                                storeIdToSend = currentUserDetails.StoreId.Value;
                                _logger.LogInformation($"StoreAdmin {loggedInUserId} requesting individual order report for their store: {storeIdToSend}");
                            }
                            else
                            {
                                TempData["Error"] = "Your Store Admin account is not associated with a store. Cannot generate report.";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            var errorContent = await userDetailsResponse.Content.ReadAsStringAsync();
                            TempData["Error"] = $"Failed to retrieve your store information: {errorContent}";
                            _logger.LogError($"Failed to get Store Admin details for individual order report: {userDetailsResponse.StatusCode} - {errorContent}");
                            return RedirectToAction("Index");
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        TempData["Error"] = "Authentication required to get your store details. Please log in again.";
                        return RedirectToAction("Login", "Auth");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"An error occurred while fetching your store details: {ex.Message}";
                        _logger.LogError(ex, "Error fetching Store Admin's store details for individual order report.");
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["Error"] = "Could not identify logged-in Store Admin. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                if (storeIdFilter.HasValue && storeIdFilter.Value != Guid.Empty)
                {
                    storeIdToSend = storeIdFilter.Value;
                    _logger.LogInformation($"SuperAdmin requesting individual order report for selected store: {storeIdToSend}");
                }
                else
                {
                    storeIdToSend = null;
                    _logger.LogInformation("SuperAdmin requesting individual order report for all stores.");
                }
            }

            // Construct the URL for the new backend Excel endpoint
            var url = $"{_baseUrl}Report/download-individual-orders-excel?startDate={startDateString}&endDate={endDateString}";
            if (storeIdToSend.HasValue)
            {
                url += $"&storeId={storeIdToSend.Value}";
            }

            _logger.LogInformation($"Requesting individual order Excel report from URL: {url}");

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var fileContents = await response.Content.ReadAsByteArrayAsync();
                    var fileName = $"IndividualOrders_{startDateString}_to_{endDateString}";
                    if (storeIdToSend.HasValue)
                    {
                        fileName += $"_Store_{storeIdToSend.Value.ToString().Substring(0, 8)}";
                    }
                    fileName += ".xlsx";

                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["Error"] = $"Failed to generate individual order report. The server responded with: {response.ReasonPhrase} - {errorContent}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading individual order report.");
                TempData["Error"] = "A network error occurred while trying to download the individual order report.";
                return RedirectToAction("Index");
            }
        }
    }
}