using System;
using System.Collections.Generic;
using System.Linq;
using System.Net; // For WebUtility.UrlEncode
using System.Security.Claims;
using System.Threading.Tasks;
using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItemManagement.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;

        public UserService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IEmailSender emailSender)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
        }

        // Read-only methods do not require explicit transactions
        public async Task<IActionResult> GetAllUsersAsync(Guid? storeId = null)
        {
            try
            {
                IQueryable<User> query = _context.Users.AsNoTracking();

                if (storeId.HasValue && storeId.Value != Guid.Empty)
                {
                    query = query.Where(u => u.StoreId == storeId.Value);
                }

                var users = await query
                    .OrderByDescending(u => u.CreateDate)
                    .Select(u => new
                    {
                        u.Id,
                        u.FName,
                        u.LName,
                        u.Email,
                        u.MobileNumber,
                        u.Profile,
                        u.Role,
                        u.IsActive,
                        u.CreateDate,
                        u.UpdateDate,
                        u.StoreId
                    })
                    .ToListAsync();

                return new OkObjectResult(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all users: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetUserAsync(Guid id)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new NotFoundObjectResult(new { Message = "User not found." });
                }

                return new OkObjectResult(new
                {
                    user.Id,
                    user.FName,
                    user.LName,
                    user.Email,
                    user.MobileNumber,
                    user.Profile,
                    user.Role,
                    user.IsActive,
                    user.CreateDate,
                    user.UpdateDate,
                    user.StoreId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user by ID: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateUserAsync(Guid id, UpdateProfileDto model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    transaction.Rollback(); // Rollback for early exit
                    return new NotFoundObjectResult(new { Message = "User not found." });
                }

                user.FName = model.FName ?? user.FName;
                user.LName = model.LName ?? user.LName;

                if (!string.IsNullOrEmpty(model.Email) && user.Email != model.Email)
                {
                    var existingUserWithNewEmail = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != id);
                    if (existingUserWithNewEmail)
                    {
                        transaction.Rollback(); // Rollback for business rule violation
                        return new ConflictObjectResult(new { Message = "Email already in use by another account." });
                    }
                    user.Email = model.Email;
                }
                else if (string.IsNullOrEmpty(model.Email))
                {
                    user.Email = model.Email ?? user.Email;
                }

                user.MobileNumber = model.MobileNumber ?? user.MobileNumber;
                user.Profile = model.Profile;

                if (!string.IsNullOrEmpty(model.Role))
                {
                    user.Role = model.Role;
                }

                user.IsActive = model.IsActive;
                user.StoreId = model.StoreId;
                user.UpdateDate = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "User updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is typically handled by re-fetching or retrying the operation.
                // It indicates that the data was modified by another process.
                transaction.Rollback(); // Rollback in case of concurrency conflict
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return new NotFoundObjectResult(new { Message = "User not found or deleted during update." });
                }
                throw; // Re-throw if not a "not found" scenario, to be handled by global error handler
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any general error
                Console.WriteLine($"Error updating user: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteUserAsync(Guid id, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    transaction.Rollback(); // Rollback for early exit
                    return new NotFoundObjectResult(new { Message = "User not found." });
                }

                var currentUserIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid currentUserIdParsed;
                bool idParsed = Guid.TryParse(currentUserIdClaim, out currentUserIdParsed);

                if (idParsed && currentUserIdParsed == id)
                {
                    transaction.Rollback(); // Rollback for business rule violation
                    return new BadRequestObjectResult(new { Message = "You cannot delete your own account." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any general error
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateUserAsync(CreateUserDto model, ClaimsPrincipal? userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                // 1. Check if user already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Email == model.Email);
                if (existingUser)
                {
                    transaction.Rollback(); // Rollback for business rule violation
                    return new ConflictObjectResult(new { Message = "Email already in use." });
                }

                // 2. Validate Store (if provided)
                string? storeName = null;
                if (model.StoreId.HasValue)
                {
                    var store = await _context.Stores
                        .FirstOrDefaultAsync(s => s.Id == model.StoreId.Value);

                    if (store == null)
                    {
                        transaction.Rollback(); // Rollback for business rule violation
                        return new BadRequestObjectResult(new { Message = "Invalid Store ID provided." });
                    }
                    storeName = store.Name; // Not directly used in DB operation, but kept for context
                }

                // 3. Create user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FName = model.FName,
                    LName = model.LName,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    Profile = model.Profile,
                    Role = model.Role,
                    StoreId = model.StoreId,
                    IsActive = model.IsActive,
                    CreateDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    CreatedBy = userClaims?.Identity?.Name ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value ?? "System",
                    Password = string.Empty, // No password now
                    Token = Guid.NewGuid().ToString()
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // Save user creation within the transaction

                // 4. Generate password setup link (Frontend URL should be configured)
                var frontendUrl = "https://localhost:7285/Auth/SetPassword"; // Replace with production URL
                var passwordSetupLink = $"{frontendUrl}?userId={user.Id}&token={WebUtility.UrlEncode(user.Token)}";

                // 5. Construct a professional email message
                string emailBody = $@"
                    <p>Hi <strong>{user.FName}</strong>,</p>
                    <p>Welcome aboard! 🎉</p>
                    <p>Your account has been successfully created</p>
                    <p>To activate your account and set your password, please click the button below:</p>
                    <p><a href='{passwordSetupLink}' style='background-color:#007bff;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;'>Set Your Password</a></p>
                    <p>If you did not expect this email, please ignore it.</p>
                    <p>Regards,<br/>Your Company Team</p>
                ";

                // 6. Send email (This is an external operation, typically outside the DB transaction's critical path)
                // If email sending failure should rollback DB, then the email logic needs to be within the try block
                // and its failure caught, but usually, DB integrity is prioritized.
                await _emailSender.SendEmailAsync(user.Email, "Your Account Has Been Created – Set Your Password", emailBody);

                await transaction.CommitAsync(); // Commit transaction on success

                // 7. Return response
                return new CreatedAtRouteResult(
                    "GetUserById",
                    new { id = user.Id },
                    new
                    {
                        Message = "User created successfully. A welcome email has been sent to set the password.",
                        User = new
                        {
                            user.Id,
                            user.FName,
                            user.LName,
                            user.Email,
                            user.MobileNumber,
                            user.Profile,
                            user.Role,
                            user.IsActive,
                            user.CreateDate,
                            user.UpdateDate,
                            user.StoreId
                        }
                    });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any general error
                Console.WriteLine($"Error creating user: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}