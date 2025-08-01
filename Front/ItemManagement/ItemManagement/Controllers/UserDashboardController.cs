using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ItemManagement.Models.ViewModels;
using System;
using System.Linq;

namespace ItemManagement.Controllers
{
    public class UserDashboardController : Controller
    {
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("UserSession");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Extract role claim
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            ViewBag.Role = roleClaim;

            // Extract FName and LName claims
            var fName = jwtToken.Claims.FirstOrDefault(c => c.Type == "FName")?.Value;
            var lName = jwtToken.Claims.FirstOrDefault(c => c.Type == "LName")?.Value;
            ViewBag.FName = fName;
            ViewBag.LName = lName;

            // Prepare API call to backend
            var apiBaseUrl = "https://localhost:7060"; // Change to your backend base URL if different
            string apiUrl = $"{apiBaseUrl}/api/dashboard/counts?role={roleClaim}";

            // If StoreAdmin, add storeId from claims
            if (roleClaim == "StoreAdmin")
            {
                var storeId = jwtToken.Claims.FirstOrDefault(c => c.Type == "StoreId")?.Value;
                if (!string.IsNullOrEmpty(storeId))
                {
                    apiUrl += $"&storeId={storeId}";
                }
            }

            DashboardCountsViewModel counts = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // If your backend requires JWT auth, pass the token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    counts = JsonConvert.DeserializeObject<DashboardCountsViewModel>(json);
                }
                else
                {
                    counts = new DashboardCountsViewModel(); // fallback to empty
                }
            }

            return View(counts);
        }
    }
}
