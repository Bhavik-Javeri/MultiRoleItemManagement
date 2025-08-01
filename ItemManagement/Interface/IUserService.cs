using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ItemManagement.Interface
{
    public interface IUserService
    {
    
     
        Task<IActionResult> GetAllUsersAsync(Guid? storeId = null);
        Task<IActionResult> GetUserAsync(Guid id);
        Task<IActionResult> UpdateUserAsync(Guid id, UpdateProfileDto model);
        Task<IActionResult> DeleteUserAsync(Guid id, ClaimsPrincipal userClaims);
        // Now sends a password setup email instead of setting a password directly
        Task<IActionResult> CreateUserAsync(CreateUserDto model, ClaimsPrincipal? userClaims);
    }
}