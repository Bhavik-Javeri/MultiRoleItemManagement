using ItemManagement.Interface;
using ItemManagement.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ItemManagement.Controllers
{
    [Route("api/stores")]
    [ApiController]
    //[Authorize(Roles = "SuperAdmin,StoreAdmin")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IStoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        // ✅ GET all stores
        [HttpGet("allstores")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetStores()
        {
            try
            {
                return await _storeService.GetStoresAsync(User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stores");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving stores." });
            }
        }

        // ✅ GET store by ID
        [HttpGet("storedetails/{storeId:guid}")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetStoreById(Guid storeId)
        {
            try
            {
                return await _storeService.GetStoreByIdAsync(storeId, User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving store details for {storeId}");
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the store." });
            }
        }

        // ✅ POST add new store
        [HttpPost("addstore")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddStore([FromBody] StoreDto storeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return await _storeService.AddStoreAsync(storeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new store");
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the store." });
            }
        }

        // ✅ PUT update existing store
        [HttpPut("updatestore/{storeId:guid}")]
        //[Authorize(Roles = "SuperAdmin,StoreAdmin")]
        public async Task<IActionResult> UpdateStore(Guid storeId, [FromBody] StoreDto storeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return await _storeService.UpdateStoreAsync(storeId, storeDto, User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating store {storeId}");
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the store." });
            }
        }

        // ✅ DELETE store
        [HttpDelete("{storeId:guid}")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteStore(Guid storeId)
        {
            try
            {
                return await _storeService.DeleteStoreAsync(storeId, User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting store {storeId}");
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the store." });
            }
        }
    }
}
