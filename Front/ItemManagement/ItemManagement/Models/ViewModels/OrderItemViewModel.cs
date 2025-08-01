namespace ItemManagement.Models.ViewModels
{
    public class OrderItemViewModel
    {
        public Guid ItemId { get; set; }
        public string? ItemName { get; set; } // Optional, for order details
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
} 