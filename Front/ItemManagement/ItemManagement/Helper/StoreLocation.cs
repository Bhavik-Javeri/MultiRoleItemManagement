using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ItemManagement.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Diagnostics;

namespace ItemManagement.Helper
{

    public class StoreLocation
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl = "https://localhost:7060/api/";
        private readonly JsonSerializerOptions _jsonOptions;

        public StoreLocation(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private HttpClient GetAuthorizedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return httpClient;
        }

        public async Task<string> GetCountryNameAsync(int countryId)
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/countries/{countryId}";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var country = JsonSerializer.Deserialize<Country>(json, _jsonOptions);
                    return country?.Name ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Country Fetch Error]: {ex.Message}");
            }

            return "Unknown";
        }

        public async Task<string> GetStateNameAsync(int stateId)
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/states/{stateId}";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var state = JsonSerializer.Deserialize<State>(json, _jsonOptions);
                    return state?.Name ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[State Fetch Error]: {ex.Message}");
            }

            return "Unknown";
        }

        public async Task<string> GetCityNameAsync(int cityId)
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/cities/{cityId}";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var city = JsonSerializer.Deserialize<City>(json, _jsonOptions);
                    return city?.Name ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[City Fetch Error]: {ex.Message}");
            }

            return "Unknown";
        }

        public async Task<List<Country>> GetAllCountriesAsync()
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/Countrys";
                Debug.WriteLine($"Fetching countries from: {url}");
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Countries response: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var countries = JsonSerializer.Deserialize<List<Country>>(responseContent, _jsonOptions);
                    return countries ?? new List<Country>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Countries Fetch Error]: {ex.Message}");
            }

            return new List<Country>();
        }

        public async Task<List<State>> GetStatesByCountryAsync(int countryId)
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/states?countryId={countryId}";
                Debug.WriteLine($"StoreLocation: Fetching states from primary backend API: {url}");
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"StoreLocation: States raw API response: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var states = JsonSerializer.Deserialize<List<State>>(responseContent, _jsonOptions);
                    if (states == null || !states.Any())
                    {
                        Debug.WriteLine("StoreLocation: No states found for countryId: " + countryId + " or deserialization resulted in empty list.");
                    }
                    return states ?? new List<State>();
                }
                else
                {
                    Debug.WriteLine($"StoreLocation: Failed to fetch states from primary backend API: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[States Fetch Error in StoreLocation]: {ex.Message}");
                Debug.WriteLine($"[States Fetch Error StackTrace]: {ex.StackTrace}");
            }

            return new List<State>();
        }

        public async Task<List<City>> GetCitiesByStateAsync(int stateId)
        {
            try
            {
                var httpClient = GetAuthorizedHttpClient();
                var url = $"{_baseUrl}Cascading/cities?stateId={stateId}";
                Debug.WriteLine($"StoreLocation: Fetching cities from primary backend API: {url}");
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"StoreLocation: Cities raw API response: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var cities = JsonSerializer.Deserialize<List<City>>(responseContent, _jsonOptions);
                    if (cities == null || !cities.Any())
                    {
                        Debug.WriteLine("StoreLocation: No cities found for stateId: " + stateId + " or deserialization resulted in empty list.");
                    }
                    return cities ?? new List<City>();
                }
                else
                {
                    Debug.WriteLine($"StoreLocation: Failed to fetch cities from primary backend API: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cities Fetch Error in StoreLocation]: {ex.Message}");
                Debug.WriteLine($"[Cities Fetch Error StackTrace]: {ex.StackTrace}");
            }

            return new List<City>();
        }
    }
}
