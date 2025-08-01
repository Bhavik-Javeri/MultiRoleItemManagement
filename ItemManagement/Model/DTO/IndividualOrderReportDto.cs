using System;
using ItemManagement.Model;

namespace ItemManagement.Model.DTO
{
    public class IndividualOrderReportDto
    {
        public Guid OrderId { get; set; }
        public string UserName { get; set; } // Changed from UserId to UserName
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public string Address { get; set; }
        public string Pincode { get; set; }
    }
} 