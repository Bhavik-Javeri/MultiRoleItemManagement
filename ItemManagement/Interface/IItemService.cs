using System;
using System.Security.Claims;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Mvc;


namespace ItemManagement.Interface
{
    public interface IItemService
    {
        // Get items: filter by category, and optionally by storeId (for Store Admins)
        Task<IActionResult> GetItemsAsync(ItemFilterDto? filterDto, Guid? storeId, ClaimsPrincipal userClaims);

        // Get a single item
        Task<IActionResult> GetItemAsync(Guid id, ClaimsPrincipal userClaims);

        // Create a new item
        Task<IActionResult> CreateItemAsync(ItemDto itemDto, ClaimsPrincipal userClaims);

        // Update an existing item
        Task<IActionResult> UpdateItemAsync(Guid id, ItemDto itemDto, ClaimsPrincipal userClaims);

        // Delete an item
        Task<IActionResult> DeleteItemAsync(Guid id, ClaimsPrincipal userClaims);

        // Removed SearchItemsAsync as GetItemsAsync can handle filtering based on ItemFilterDto.
        // If a more complex text-based search is needed, it would be added back with different parameters.
    }
}