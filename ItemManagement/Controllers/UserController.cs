using ItemManagement.Interface;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System; // Added for Exception
using System.Security.Claims;
using System.Threading.Tasks;

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetAllUser")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> GetAllUsers(Guid? storeId )
        {
            try
            {
                return await _userService.GetAllUsersAsync(storeId);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error retrieving all users: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving all users." });
            }
        }

        [HttpGet("getsingleuser/{id:guid}", Name = "GetUserById")] // MODIFIED LINE: Added Name = "GetUserById"
        [Authorize(Roles = "SuperAdmin,StoreAdmin,User")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                return await _userService.GetUserAsync(id);
            }
            catch (Exception ex)
            {
                // Log the exception details here (e.g., using ILogger)
                // Console.WriteLine($"Error retrieving user {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the user." });
            }
        }

        [HttpPut("updateUser/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                return await _userService.UpdateUserAsync(id, model);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error updating user {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the user." });
            }
        }

        [HttpDelete("DeleteUser/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                return await _userService.DeleteUserAsync(id, User);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error deleting user {id}: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the user." });
            }
        }

        [HttpPost("CreateUser")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                return await _userService.CreateUserAsync(model, User);
            }
            catch (Exception ex)
            {
                // Log the exception details here
                // Console.WriteLine($"Error creating user: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the user." });
            }
        }
    }
}