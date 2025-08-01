using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ItemManagement.Interface;
using ItemManagement.Model;
using ItemManagement.Model.DTO;
using Microsoft.EntityFrameworkCore;

namespace ItemManagement.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        // MODIFIED: Changed 'date' to 'startDate' and 'endDate' and updated filtering logic
        public async Task<List<DailyOrderReportDto>> GetDailyOrderReportAsync(DateTime startDate, DateTime endDate, Guid? storeId)
        {
            var query = _context.Orders
                .Include(o => o.Store)
                // Filter orders where OrderDate is within the specified range (inclusive)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date);

            if (storeId.HasValue)
                query = query.Where(o => o.StoreId == storeId);

            // OPTIONAL: If you want DailyOrderReportDto to also only show Approved orders, add this line:
            // query = query.Where(o => o.Status == OrderStatus.Approved);

            var grouped = await query
                .GroupBy(o => new { o.OrderDate.Date, o.StoreId, o.Store.Name })
                .Select(g => new DailyOrderReportDto
                {
                    Date = g.Key.Date,
                    StoreId = g.Key.StoreId,
                    StoreName = g.Key.Name,
                    TotalOrders = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    Status = g.Select(o => o.Status).FirstOrDefault() // This might be misleading for grouped orders with mixed statuses
                })
                .ToListAsync();
            return grouped;
        }

        // MODIFIED: Changed 'date' to 'startDate' and 'endDate' and updated filtering logic
        public async Task<List<DailyItemTransactionReportDto>> GetDailyItemTransactionReportAsync(DateTime startDate, DateTime endDate, Guid? storeId)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Item)
                // Filter by OrderDate.Date within the specified range (inclusive) AND OrderStatus.Approved
                .Where(oi => oi.Order.OrderDate.Date >= startDate.Date &&
                             oi.Order.OrderDate.Date <= endDate.Date &&
                             oi.Order.Status == OrderStatus.Approved);

            if (storeId.HasValue)
                query = query.Where(oi => oi.Order.StoreId == storeId);

            var grouped = await query
                .GroupBy(oi => new { oi.Order.OrderDate.Date, oi.ItemId, ItemName = oi.Item.Name, oi.Order.StoreId, StoreName = oi.Order.Store.Name })
                .Select(g => new DailyItemTransactionReportDto
                {
                    Date = g.Key.Date,
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.ItemName,
                    StoreId = g.Key.StoreId,
                    StoreName = g.Key.StoreName,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    TotalSales = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .ToListAsync();
            return grouped;
        }

        // NEW: Return all individual orders for a store within a date range
        public async Task<List<IndividualOrderReportDto>> GetIndividualOrdersReportAsync(DateTime startDate, DateTime endDate, Guid? storeId)
        {
            var query = _context.Orders
                .Include(o => o.Store)
                .Include(o => o.User)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date);

            if (storeId.HasValue)
                query = query.Where(o => o.StoreId == storeId);

            var orders = await query
                .Select(o => new IndividualOrderReportDto
                {
                    OrderId = o.OrderId,
                    UserName = o.User.FName + " " + o.User.LName,
                    StoreId = o.StoreId,
                    StoreName = o.Store.Name,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    Address = o.Address,
                    Pincode = o.Pincode
                })
                .ToListAsync();
            return orders;
        }
    }
}