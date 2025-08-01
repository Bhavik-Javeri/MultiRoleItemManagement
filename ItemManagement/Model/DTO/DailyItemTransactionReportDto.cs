using System;

namespace ItemManagement.Model.DTO
{
    public class DailyItemTransactionReportDto
    {
        public DateTime Date { get; set; }
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public Guid? StoreId { get; set; }
        public string StoreName { get; set; }
    }
} 