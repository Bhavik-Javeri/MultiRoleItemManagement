using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using ItemManagement.Models.ViewModels;
using ItemManagement.Models;
using ItemManagement.Helper;
using System.Diagnostics;

namespace ItemManagement.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LocationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly StoreLocation _storeLocation;

        public LocationController(IConfiguration configuration, IHttpClientFactory httpClientFactory, StoreLocation storeLocation)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _storeLocation = storeLocation;
        }

        private async Task<HttpClient> GetAuthorizedHttpClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet("GetCountries")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _storeLocation.GetAllCountriesAsync();
                return Json(countries);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocationController: Error in GetCountries: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet("GetStates/{countryId:int}")]
        public async Task<IActionResult> GetStates(int countryId)
        {
            try
            {
                var states = await _storeLocation.GetStatesByCountryAsync(countryId);
                return Json(states);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocationController: Error in GetStates: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet("GetCities/{stateId:int}")]
        public async Task<IActionResult> GetCities(int stateId)
        {
            try
            {
                var cities = await _storeLocation.GetCitiesByStateAsync(stateId);
                return Json(cities);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocationController: Error in GetCities: {ex.Message}");
                return Json(new List<object>());
            }
        }
    }
}
