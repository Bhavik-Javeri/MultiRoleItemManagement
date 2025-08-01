using ItemManagement.Model;

namespace ItemManagement.Model.DTO
{
    public class OrderItemDto
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public static OrderItemDto Mapping(OrderItem orderItem)
        {
            return new OrderItemDto
            {
                ItemId = orderItem.ItemId,
                Quantity = orderItem.Quantity,
                Price   = orderItem.Price
            };
        }
    }
}
