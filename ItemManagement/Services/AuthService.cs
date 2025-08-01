using ItemManagement.Interface;
using ItemManagement.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net; // For WebUtility.UrlEncode

namespace ItemManagement.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenServices _jwtTokenServices;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;

        public AuthService(ApplicationDbContext context, JwtTokenServices JwtTokenServices, IPasswordHasher<User> passwordHasher, IEmailSender emailSender)
        {
            _context = context;
            _jwtTokenServices = JwtTokenServices;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
        }

        public async Task<RegisterModel> Register([FromBody] RegisterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var existingUser = await _context.Users
                  .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("Email already registered.");
                }

                // Prevent SuperAdmin registration from public endpoint
                var role = model.Role?.Trim().ToLower() == "superadmin" ? "User" : model.Role;

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FName = model.FName,
                    LName = model.LName,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    CreateDate = DateTime.UtcNow,
                    IsActive = true,
                    Role = role,
                    Password = string.Empty, // No password at registration
                    Token = Guid.NewGuid().ToString() // Token for set password
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate password setup link (replace with production URL as needed)
                var frontendUrl = "https://localhost:7285/Auth/SetPassword";
                var passwordSetupLink = $"{frontendUrl}?userId={user.Id}&token={WebUtility.UrlEncode(user.Token)}";

                string emailBody = $@"
            <p>Hi <strong>{user.FName}</strong>,</p>
            <p>Welcome aboard! 🎉</p>
            <p>Your account has been successfully created</p>
            <p>To activate your account and set your password, please click the button below:</p>
            <p><a href='{passwordSetupLink}' style='background-color:#007bff;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;'>Set Your Password</a></p>
            <p>If you did not expect this email, please ignore it.</p>
            <p>Regards,<br/>Your Company Team</p>
        ";

                await _emailSender.SendEmailAsync(user.Email, "Your Account Has Been Created – Set Your Password", emailBody);

                await transaction.CommitAsync();
                return model; // Optionally, return a DTO with a message
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ApplicationException("An error occurred during registration.", ex);
            }
        }

        public async Task<IActionResult> Login(LoginModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var user = await _context.Users.Include(u => u.Store).FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    transaction.Rollback(); // No changes to rollback, but good practice
                    return new UnauthorizedObjectResult(new { Message = "Invalid email or password." });
                }

                if (_passwordHasher.VerifyHashedPassword(user, user.Password, model.Password) == PasswordVerificationResult.Failed)
                {
                    transaction.Rollback();
                    return new UnauthorizedObjectResult(new { Message = "Invalid email or password." });
                }

                if (!user.IsActive)
                {
                    transaction.Rollback();
                    return new UnauthorizedObjectResult(new { Message = "Account is inactive. Please contact support." });
                }

                if (user.StoreId.HasValue && user.Store != null && !user.Store.IsActive)
                {
                    transaction.Rollback();
                    return new UnauthorizedObjectResult(new { Message = "Currently your store is not active. Please come back after some time." });
                }

                var token = _jwtTokenServices.GenerateToken(user.Id, user.Role, user.StoreId, user.FName, user.LName);
                user.Token = token;
                _context.Users.Update(user);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Token = token, Message = "Login successful." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback transaction on any error
                return new StatusCodeResult(500); // Internal Server Error
            }
        }

        public async Task<IActionResult> Logout(ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new { Message = "Invalid user ID format in token or user not authenticated." });
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult(new { Message = "User not found in database." });
                }

                user.Token = null;
                _context.Users.Update(user);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "User logged out successfully. Token cleared from database." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback transaction on any error
                return new StatusCodeResult(500); // Internal Server Error
            }
        }

        public async Task<IActionResult> SetPasswordAsync(Guid userId, string token, string newPassword)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Token == token);
                if (user == null)
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new { Message = "Invalid or expired token." });
                }
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new { Message = "Password must be at least 6 characters long." });
                }
                user.Password = _passwordHasher.HashPassword(user, newPassword);
                user.Token = null; // Invalidate the token after use
                user.UpdateDate = DateTime.UtcNow;
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Password set successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback transaction on any error
                return new StatusCodeResult(500); // Internal Server Error
            }
        }
    }
}