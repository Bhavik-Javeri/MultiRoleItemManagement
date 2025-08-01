using Microsoft.IdentityModel.JsonWebTokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ItemManagement.Services
{
    public class ItemService : IItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService; // To get user details if needed for CreatedBy/UpdatedBy

        public ItemService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        private Guid GetUserIdFromClaims(ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst(JwtRegisteredClaimNames.NameId) ??
                              userClaims.FindFirst(ClaimTypes.NameIdentifier) ??
                              userClaims.FindFirst("sub"); // Fallback for JWT standard 'sub' claim

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                throw new UnauthorizedAccessException("User ID claim not found or is invalid.");
            }
            return userId;
        }

        private string GetUserNameFromClaims(ClaimsPrincipal userClaims)
        {
            return userClaims.Identity?.Name ?? userClaims.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        }

        // Helper to convert DTO to Entity for creation
        private Item ToEntity(ItemDto itemDto, ClaimsPrincipal userClaims)
        {
            Guid effectiveStoreId;
            var currentUserId = GetUserIdFromClaims(userClaims);
            var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "SuperAdmin")
            {
                if (!itemDto.StoreId.HasValue)
                {
                    throw new ArgumentException("Super Admin must specify a StoreId when creating an item.");
                }
                effectiveStoreId = itemDto.StoreId.Value;
            }
            else if (currentUserRole == "StoreAdmin")
            {
                var storeAdminUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == currentUserId);
                if (storeAdminUser == null || !storeAdminUser.StoreId.HasValue)
                {
                    throw new UnauthorizedAccessException("Store Admin is not associated with a store.");
                }
                if (itemDto.StoreId.HasValue && itemDto.StoreId.Value != storeAdminUser.StoreId.Value)
                {
                    throw new UnauthorizedAccessException("Store Admin cannot create items for another store.");
                }
                effectiveStoreId = storeAdminUser.StoreId.Value;
            }
            else
            {
                throw new UnauthorizedAccessException("Only Super Admins and Store Admins can create items.");
            }

            if (!_context.Stores.Any(s => s.Id == effectiveStoreId))
            {
                throw new ArgumentException($"Store with ID {effectiveStoreId} not found.");
            }

            if (!Enum.TryParse<Category>(itemDto.categoryType, true, out Category parsedType))
            {
                throw new ArgumentException($"Invalid Item Type: {itemDto.categoryType}. Must be 'Veg', 'NonVeg', or 'Egg'.");
            }

            return new Item
            {
                Id = Guid.NewGuid(),
                Name = itemDto.Name,
                Description = itemDto.Description,
                Price = itemDto.Price,
                Quantity = itemDto.Quantity,
                ImageUrl = itemDto.ImageUrl,
                categoryType = parsedType,
                StoreId = effectiveStoreId,
                IsActive = itemDto.IsActive,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                CreatedBy = GetUserNameFromClaims(userClaims),
                UpdatedBy = GetUserNameFromClaims(userClaims)
            };
        }

        // Helper to update Entity from DTO for existing item
        private void UpdateEntity(Item existingItem, ItemDto itemDto, ClaimsPrincipal userClaims)
        {
            var currentUserId = GetUserIdFromClaims(userClaims);
            var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole == "StoreAdmin")
            {
                var storeAdminUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == currentUserId);
                if (storeAdminUser == null || !storeAdminUser.StoreId.HasValue || existingItem.StoreId != storeAdminUser.StoreId.Value)
                {
                    throw new UnauthorizedAccessException("Store Admin can only update items in their assigned store.");
                }
            }
            else if (currentUserRole != "SuperAdmin")
            {
                throw new UnauthorizedAccessException("Only Super Admins and Store Admins can update items.");
            }

            existingItem.Name = itemDto.Name ?? existingItem.Name;
            existingItem.Description = itemDto.Description ?? existingItem.Description;
            existingItem.Price = itemDto.Price;
            existingItem.Quantity = itemDto.Quantity;
            existingItem.ImageUrl = itemDto.ImageUrl;
            existingItem.IsActive = itemDto.IsActive;

            if (!string.IsNullOrEmpty(itemDto.categoryType))
            {
                if (Enum.TryParse<Category>(itemDto.categoryType, true, out Category parsedType))
                {
                    existingItem.categoryType = parsedType;
                }
                else
                {
                    throw new ArgumentException($"Invalid Item Type: {itemDto.categoryType}. Must be 'Veg', 'NonVeg', or 'Egg'.");
                }
            }

            if (itemDto.StoreId.HasValue && existingItem.StoreId != itemDto.StoreId.Value)
            {
                if (currentUserRole == "SuperAdmin")
                {
                    if (!_context.Stores.Any(s => s.Id == itemDto.StoreId.Value))
                    {
                        throw new ArgumentException($"New Store with ID {itemDto.StoreId.Value} not found.");
                    }
                    existingItem.StoreId = itemDto.StoreId.Value;
                }
                else
                {
                    throw new UnauthorizedAccessException("Only Super Admins can reassign items to different stores.");
                }
            }

            existingItem.UpdateDate = DateTime.UtcNow;
            existingItem.UpdatedBy = GetUserNameFromClaims(userClaims);
        }

        public async Task<IActionResult> GetItemsAsync(ItemFilterDto? filterDto, Guid? storeId, ClaimsPrincipal userClaims)
        {
            var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = GetUserIdFromClaims(userClaims);

            IQueryable<Item> query = _context.Items
                                             .Include(i => i.Store)
                                             .AsNoTracking()
                                             .Where(i => !i.IsDelete);

            if (currentUserRole == "StoreAdmin")
            {
                var storeAdminUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (storeAdminUser == null || !storeAdminUser.StoreId.HasValue)
                {
                    return new UnauthorizedObjectResult(new { Message = "Store Admin is not associated with a store." });
                }
                query = query.Where(i => i.StoreId == storeAdminUser.StoreId.Value);
            }
            else if (storeId.HasValue)
            {
                query = query.Where(i => i.StoreId == storeId.Value);
            }

            if (filterDto?.categoryType.HasValue == true)
            {
                query = query.Where(i => i.categoryType == filterDto.categoryType.Value);
            }

            try
            {
                var items = await query.ToListAsync();
                var itemDtos = ItemDto.FromEntities(items);

                return new OkObjectResult(itemDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting items: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetItemAsync(Guid id, ClaimsPrincipal userClaims)
        {
            var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = GetUserIdFromClaims(userClaims);

            var item = await _context.Items.Include(i => i.Store).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return new NotFoundObjectResult(new { Message = "Item not found." });
            }

            if (currentUserRole == "StoreAdmin")
            {
                var storeAdminUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (storeAdminUser == null || !storeAdminUser.StoreId.HasValue || item.StoreId != storeAdminUser.StoreId.Value)
                {
                    return new UnauthorizedObjectResult(new { Message = "Store Admin can only view items in their assigned store." });
                }
            }
            else if (currentUserRole != "SuperAdmin" && currentUserRole != "User")
            {
                return new UnauthorizedObjectResult(new { Message = "Access denied." });
            }

            return new OkObjectResult(ItemDto.FromEntity(item));
        }

        public async Task<IActionResult> CreateItemAsync(ItemDto itemDto, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole != "SuperAdmin" && currentUserRole != "StoreAdmin")
                {
                    transaction.Rollback();
                    return new ForbidResult();
                }

                if (!ModelStateIsValid(itemDto))
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new { Message = "Invalid item data provided.", Errors = GetValidationErrors(itemDto) });
                }

                var item = ToEntity(itemDto, userClaims);

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync(); // Commit transaction on success
                return new CreatedAtRouteResult(
                    "GetItemById",
                    new { id = item.Id },
                    ItemDto.FromEntity(item)
                );
            }
            catch (ArgumentException ex)
            {
                transaction.Rollback(); // Rollback on specific known errors
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                transaction.Rollback(); // Rollback on specific known errors
                return new UnauthorizedObjectResult(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any general error
                Console.WriteLine($"Error creating item: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateItemAsync(Guid id, ItemDto itemDto, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole != "SuperAdmin" && currentUserRole != "StoreAdmin")
                {
                    transaction.Rollback();
                    return new ForbidResult();
                }

                if (!ModelStateIsValid(itemDto))
                {
                    transaction.Rollback();
                    return new BadRequestObjectResult(new { Message = "Invalid item data provided.", Errors = GetValidationErrors(itemDto) });
                }

                var item = await _context.Items.FindAsync(id);

                if (item == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult(new { Message = "Item not found." });
                }

                UpdateEntity(item, itemDto, userClaims);

                _context.Items.Update(item);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Item updated successfully.", UpdatedItem = ItemDto.FromEntity(item) });
            }
            catch (ArgumentException ex)
            {
                transaction.Rollback();
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                transaction.Rollback();
                return new UnauthorizedObjectResult(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error updating item: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteItemAsync(Guid id, ClaimsPrincipal userClaims)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var currentUserRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole != "SuperAdmin" && currentUserRole != "StoreAdmin")
                {
                    transaction.Rollback();
                    return new ForbidResult();
                }

                var item = await _context.Items.FindAsync(id);

                if (item == null)
                {
                    transaction.Rollback();
                    return new NotFoundObjectResult(new { Message = "Item not found." });
                }

                if (currentUserRole == "StoreAdmin")
                {
                    var currentUserId = GetUserIdFromClaims(userClaims);
                    var storeAdminUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (storeAdminUser == null || !storeAdminUser.StoreId.HasValue || item.StoreId != storeAdminUser.StoreId.Value)
                    {
                        transaction.Rollback();
                        return new UnauthorizedObjectResult(new { Message = "Store Admin can only delete items in their assigned store." });
                    }
                }

                var items = _context.Items.FirstOrDefault(x => x.Id == item.Id);
                items.IsDelete = true; // Soft delete
                await _context.SaveChangesAsync();

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                Console.WriteLine($"Error deleting item: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        private bool ModelStateIsValid<T>(T model)
        {
            var validationContext = new ValidationContext(model, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            return Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        }

        private IEnumerable<string> GetValidationErrors<T>(T model)
        {
            var validationContext = new ValidationContext(model, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
            return validationResults.Select(r => r.ErrorMessage ?? "Validation error");
        }
    }
}