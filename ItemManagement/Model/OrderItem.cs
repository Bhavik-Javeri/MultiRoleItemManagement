using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItemManagement.Model
{
    public class OrderItem
    {
       

        [Required]
        public Guid OrderId { get; set; } 

        [Required]
        public Guid ItemId { get; set; } 

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } 

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; } 

        //Relationship
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("ItemId")]
        public Item? Item { get; set; }

       
    }
}
