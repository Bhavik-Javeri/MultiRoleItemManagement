namespace ItemManagement.Model
{
    public class CartItem
    {
        public Guid? CartId { get; set; }
        public Cart Cart { get; set; }
        public Guid ItemId { get; set; }
        public Item Item { get; set; }
        public decimal Price {  get; set; }
        public int Quantity { get; set; }
    }
}
