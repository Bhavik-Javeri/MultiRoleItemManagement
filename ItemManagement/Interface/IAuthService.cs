using System.Security.Claims;
using ItemManagement.Model;
using Microsoft.AspNetCore.Mvc;

namespace ItemManagement.Interface
{
    public interface IAuthService
    {
        Task<RegisterModel> Register([FromBody] RegisterModel model);
        Task<IActionResult> Login(LoginModel model);
        Task<IActionResult> Logout(ClaimsPrincipal userClaims);
        Task<IActionResult> SetPasswordAsync(Guid userId, string token, string newPassword);
    }
}
