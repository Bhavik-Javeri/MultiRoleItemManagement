using ItemManagement.Interface;
using ItemManagement.Model; // Assuming Country, State, City are in this namespace
using Microsoft.AspNetCore.Mvc;
using System; // Added for Exception
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CascadingController : ControllerBase
    {
        private readonly ICascadingService _cascadingService;

        public CascadingController(ICascadingService cascadingService)
        {
            _cascadingService = cascadingService;
        }

        [HttpGet("Countrys")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _cascadingService.GetCountriesAsync();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error retrieving countries: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving countries." });
            }
        }

        [HttpGet("States")]
        public async Task<IActionResult> GetStates([FromQuery] int countryId)
        {
            try
            {
                var states = await _cascadingService.GetStatesAsync(countryId);
                return Ok(states);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error retrieving states for countryId {countryId}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving states." });
            }
        }

        [HttpGet("Cities")]
        public async Task<IActionResult> GetCities([FromQuery] int stateId)
        {
            try
            {
                var cities = await _cascadingService.GetCitiesAsync(stateId);
                return Ok(cities);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error retrieving cities for stateId {stateId}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving cities." });
            }
        }

        [HttpGet("countries/{id}")]
        public async Task<ActionResult<Country>> GetCountryById(int id)
        {
            try
            {
                var country = await _cascadingService.GetCountryByIdAsync(id);
                if (country == null)
                {
                    return NotFound(new { Message = $"Country with ID {id} not found." });
                }
                return Ok(country);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error retrieving country by ID {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the country." });
            }
        }

        [HttpGet("states/{id}")]
        public async Task<ActionResult<State>> GetStateById(int id)
        {
            try
            {
                var state = await _cascadingService.GetStateByIdAsync(id);
                if (state == null)
                {
                    return NotFound(new { Message = $"State with ID {id} not found." });
                }
                return Ok(state);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error retrieving state by ID {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the state." });
            }
        }

        [HttpGet("cities/{id}")]
        public async Task<ActionResult<City>> GetCityById(int id)
        {
            try
            {
                var city = await _cascadingService.GetCityByIdAsync(id);
                if (city == null)
                {
                    return NotFound(new { Message = $"City with ID {id} not found." });
                }
                return Ok(city);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error retrieving city by ID {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the city." });
            }
        }
    }
}