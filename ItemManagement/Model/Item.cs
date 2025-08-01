using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItemManagement.Model
{
    // Define an enum for ItemType
    public enum Category
    {
        Veg,
        NonVeg,
        Egg
    }

    public class Item
    {
        [Key]
        public Guid Id { get; set; } // Confirmed GUID as per requirement

        [Required(ErrorMessage = "Item Name is required.")]
        [StringLength(255, ErrorMessage = "Item Name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Column(TypeName = "decimal(18,2)")] // Explicitly define decimal precision for database
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; } // Path or URL to the item image

        [Required(ErrorMessage = "Item Type is required.")]
        public Category categoryType { get; set; } // New property for item type

        [Required]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdateDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
        public bool IsDelete { get; set; } = false;

        // Foreign key to Store
        public Guid StoreId { get; set; } // Item must belong to a store

        [ForeignKey("StoreId")]
        public Store? Store { get; set; } // Navigation property

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public ICollection<CartItem>? CartItems { get; set; } // Many-to-many through CartItem
        public ICollection<OrderItem>? OrderItems { get; set; } // Many-to-many through OrderItem
    }
}
