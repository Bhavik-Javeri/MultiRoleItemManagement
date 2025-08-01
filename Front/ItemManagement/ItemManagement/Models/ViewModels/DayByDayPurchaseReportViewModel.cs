using System;
using System.Collections.Generic;
using ItemManagement.Models.ViewModels;

namespace ItemManagement.Models
{
    public class DayByDayPurchaseReportViewModel
    {
        public DateTime Date { get; set; }
        public string StoreName { get; set; }
        public decimal TotalSales { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
    }
}