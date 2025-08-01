using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model.DTO
{
    public class OrderDto
    {
        [Required]
        public Guid StoreId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 200 characters")]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits")]
        public string Pincode { get; set; }

        public ItemManagement.Model.OrderStatus Status { get; set; }
    }
}
