using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Ensure this is included

namespace ItemManagement.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StoreService> _logger;

        public StoreService(ApplicationDbContext context, ILogger<StoreService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Read-only methods do not require explicit transactions
        public async Task<IActionResult> GetStoresAsync(ClaimsPrincipal user)
        {
            try
            {
                var stores = await _context.Stores
                    .Include(s => s.Users)
                    .OrderByDescending(s => s.CreateDate)
                    .ToListAsync();

                var storeDtos = StoreDto.Mapping(stores);

                return new OkObjectResult(storeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetStoreByIdAsync(Guid storeId, ClaimsPrincipal user)
        {
            try
            {
                var store = await _context.Stores
                    .Include(s => s.Users)
                    .FirstOrDefaultAsync(s => s.Id == storeId);

                if (store == null)
                    return new NotFoundObjectResult(new { Message = "Store not found." });

                var storeDto = StoreDto.Mapping(store);
                return new OkObjectResult(storeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching store ID: {storeId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> AddStoreAsync(StoreDto storeDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var newStore = StoreDto.Mapping(storeDto);

                _context.Stores.Add(newStore);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Store added successfully", StoreId = newStore.Id });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                _logger.LogError(ex, "Error adding store");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateStoreAsync(Guid storeId, StoreDto storeDto, ClaimsPrincipal user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var store = await _context.Stores.FindAsync(storeId);
                if (store == null)
                {
                    transaction.Rollback(); // Rollback for early exit
                    return new NotFoundObjectResult(new { Message = "Store not found." });
                }

                // Update fields
                store.Name = storeDto.Name;
                store.Address = storeDto.Address;
                store.ContactNumber = storeDto.ContactNumber;
                store.Email = storeDto.Email;
                store.CountryId = storeDto.CountryId;
                store.StateId = storeDto.StateId;
                store.CityId = storeDto.CityId;
                store.Image = storeDto.ImageUrl;
                store.OpeningHours = storeDto.OpeningHours;
                store.ClosingHours = storeDto.ClosingHours;
                store.IsActive = storeDto.IsActive;
                store.UpdateDate = DateTime.UtcNow;

                _context.Stores.Update(store);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Store updated successfully" });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                _logger.LogError(ex, $"Error updating store ID: {storeId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteStoreAsync(Guid storeId, ClaimsPrincipal user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Start transaction
            try
            {
                var store = await _context.Stores.FindAsync(storeId);
                if (store == null)
                {
                    transaction.Rollback(); // Rollback for early exit
                    return new NotFoundObjectResult(new { Message = "Store not found." });
                }

                _context.Stores.Remove(store);
                await _context.SaveChangesAsync(); // Save changes within the transaction

                await transaction.CommitAsync(); // Commit transaction on success
                return new OkObjectResult(new { Message = "Store deleted successfully" });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback on any error
                _logger.LogError(ex, $"Error deleting store ID: {storeId}");
                return new StatusCodeResult(500);
            }
        }
    }
}