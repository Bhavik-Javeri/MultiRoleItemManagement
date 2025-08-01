using System;
using System.Threading.Tasks; // Added for Task
using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ItemManagement.Model.DTO;

namespace ItemManagement.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtTokenServices _jwtTokenServices;

        public AuthController(IAuthService authService, JwtTokenServices jwtTokenServices)
        {
            _jwtTokenServices = jwtTokenServices;

            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Validate model state at the controller level
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.Register(model);

                if (result != null)
                {
                    return Ok(result);
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error during registration: {ex.Message}"); // For debugging, use a proper logger in production
                return StatusCode(500, new { Message = "An unexpected error occurred during registration." + ex });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // The service method should return IActionResult directly
                return await _authService.Login(model);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error during login: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred during login." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // The service method should return IActionResult directly
                return await _authService.Logout(User);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error during logout: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred during logout." });
            }
        }

        [HttpPost("SetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto model)
        {
            if (model == null || model.UserId == Guid.Empty || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { Message = "Invalid request." });
            }
            return await _authService.SetPasswordAsync(model.UserId, model.Token, model.NewPassword);
        }
    }
}