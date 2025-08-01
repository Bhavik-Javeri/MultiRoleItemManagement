using System;

namespace ItemManagement.Model.DTO
{
    public class DailyOrderReportDto
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid? StoreId { get; set; }
        public string StoreName { get; set; }
        public ItemManagement.Model.OrderStatus Status { get; set; }
    }
} 