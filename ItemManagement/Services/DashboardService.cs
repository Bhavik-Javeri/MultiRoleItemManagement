using ItemManagement.Model.DTO;
using ItemManagement.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ItemManagement.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardCountsDto> GetDashboardCountsAsync(string role, Guid? storeId = null)
        {
            _logger.LogInformation($"Calling GetDashboardCounts with role={role}, storeId={storeId}");
            var roleParam = new SqlParameter("@Role", role);
            var storeIdParam = new SqlParameter("@StoreId", storeId ?? (object)DBNull.Value);

            // Use raw SQL query to map the result to DashboardCountsDto
            var result = await _context.DashboardCounts
                .FromSqlRaw("EXEC GetDashboardCounts @Role, @StoreId", roleParam, storeIdParam)
                .AsNoTracking()
                .ToListAsync();

            var counts = result.FirstOrDefault();
            _logger.LogInformation($"SP result: TotalUsers={counts?.TotalUsers}, TotalStores={counts?.TotalStores}, TotalItems={counts?.TotalItems}, TotalOrders={counts?.TotalOrders}");
            return counts;
        }
    }
} 