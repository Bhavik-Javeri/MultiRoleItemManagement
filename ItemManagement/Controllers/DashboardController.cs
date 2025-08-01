using ItemManagement.Model.DTO;
using ItemManagement.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ItemManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("counts")]
        public async Task<ActionResult<DashboardCountsDto>> GetCounts([FromQuery] string role, [FromQuery] Guid? storeId)
        {
            var counts = await _dashboardService.GetDashboardCountsAsync(role, storeId);
            if (counts == null) return NotFound();
            return Ok(counts);
        }
    }
} 