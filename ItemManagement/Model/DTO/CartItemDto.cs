namespace ItemManagement.Model.DTO
{
    public class CartItemDto
    {
        public Guid ItemId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
