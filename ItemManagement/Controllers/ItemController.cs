using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ItemManagement.Interface;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims; // Needed for ClaimsPrincipal
using System.Threading.Tasks;
using System.Linq; // Needed for .Any() and .Select() if you return validation errors

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")] // Base route: /api/Item
    [ApiController]
    [Authorize] // All actions in this controller require authentication by default
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        
        [HttpGet("GetAllItems")]
        [Authorize(Roles = "SuperAdmin,StoreAdmin,User")] // All roles can view items
        public async Task<IActionResult> GetAllItems([FromQuery] ItemFilterDto? filterDto, [FromQuery] Guid? storeId)
        {
            // The service layer will handle the specific authorization logic (e.g., StoreAdmin only sees their items)
            return await _itemService.GetItemsAsync(filterDto, storeId, User);
        }

        [HttpGet("{id:guid}", Name = "GetItemById")] // MODIFIED LINE: Added Name = "GetItemById"
        [Authorize(Roles = "SuperAdmin,StoreAdmin,User")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            return await _itemService.GetItemAsync(id, User);
        }

        [HttpPost("CreateItem")] // e.g., /api/Item/CreateItem
        [Authorize(Roles = "SuperAdmin,StoreAdmin")] // Only SuperAdmin and StoreAdmin can create
        public async Task<IActionResult> CreateItem([FromBody] ItemDto itemDto)
        {
            // ModelState validation using DataAnnotations from ItemDto
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return await _itemService.CreateItemAsync(itemDto, User);
        }

        [HttpPut("UpdateItem/{id:guid}")] // e.g., /api/Item/UpdateItem/{guid}
        [Authorize(Roles = "SuperAdmin,StoreAdmin")] // Only SuperAdmin and StoreAdmin can update
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] ItemDto itemDto)
        {
            // Ensure ID in route matches ID in DTO (if DTO has it)
            if (itemDto.Id.HasValue && id != itemDto.Id.Value)
            {
                return BadRequest(new { Message = "Item ID mismatch." });
            }

            // ModelState validation using DataAnnotations from ItemDto
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return await _itemService.UpdateItemAsync(id, itemDto, User);
        }

        [HttpDelete("DeleteItem/{id:guid}")] // e.g., /api/Item/DeleteItem/{guid}
        [Authorize(Roles = "SuperAdmin,StoreAdmin")] // Only SuperAdmin and StoreAdmin can delete
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            return await _itemService.DeleteItemAsync(id, User);
        }
    }
}
