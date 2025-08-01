using ItemManagement.Models.ViewModels;

namespace ItemManagement.Models
{
    public class Order
    {
        public Guid StoreId { get; set; }


        public Guid? Id { get; set; }         
                                                   
        public string? StoreName { get; set; }     
        public Guid? UserId { get; set; }         
        public string? UserName { get; set; }      
        public DateTime OrderDate { get; set; }           
        public decimal? TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;  // Default status is Pending
        public string Address { get; set; }
        public string Pincode { get; set; }


        public static OrderViewModel ToViewModel(Order order)
        {
            return new OrderViewModel
            {
                Id = order.Id ?? Guid.Empty,
                StoreId = order.StoreId,
                StoreName = order.StoreName ?? string.Empty,
                UserId = order.UserId ?? Guid.Empty,
                UserName = order.UserName ?? string.Empty,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount ?? 0m,
                Status = order.Status,
                Address = order.Address ?? string.Empty,
                Pincode = order.Pincode ?? string.Empty
            };
        }
    }

}
