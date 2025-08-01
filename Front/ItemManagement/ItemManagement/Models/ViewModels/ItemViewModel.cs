using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Needed for IFormFile
using System.Text.Json.Serialization; // For JsonConverter

namespace ItemManagement.Models.ViewModels
{
    public class ItemViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Item Name is required.")]
        [StringLength(255, ErrorMessage = "Item Name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int Quantity { get; set; }

        [StringLength(500)]
        // This property will hold the URL/path of the image (used for display and API calls)
        public string? ImageUrl { get; set; }

        // This property will be used to accept a new image file upload from a form.
        // It's separate from ImageUrl because it's an IFormFile for uploading.
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [JsonConverter(typeof(JsonStringEnumConverter))] // Add this for proper enum binding from form
        // This will be the frontend enum, which aligns with the backend ItemType enum
        public Category categoryType { get; set; }

        // For display purposes (e.g., when showing items in a list or creating/editing for admin)
        [Required(ErrorMessage = "Store ID is required for an item.")] // Added Required for StoreId
        public Guid StoreId { get; set; }
        public string? StoreName { get; set; } // For displaying the associated store's name

        public bool IsActive { get; set; }
    }
}
