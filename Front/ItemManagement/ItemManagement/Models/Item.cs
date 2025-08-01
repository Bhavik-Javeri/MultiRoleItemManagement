using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Needed for IFormFile
using ItemManagement.Models.ViewModels;

namespace ItemManagement.Models
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public Guid StoreId { get; set; }
        public bool IsActive { get; set; }
        public Category categoryType { get; set; }

        public static ItemViewModel ToViewModel(Item item)
        {
            return new ItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Quantity = item.Quantity,
                ImageUrl = item.ImageUrl,
                StoreId = item.StoreId,
                IsActive = item.IsActive,
                categoryType = item.categoryType
            };
        }
    }
}
