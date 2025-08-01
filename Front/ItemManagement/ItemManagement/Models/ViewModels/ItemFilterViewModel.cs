using System; // For Guid
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem
using System.ComponentModel.DataAnnotations;
//using ItemManagement.Models.ViewModel; // Add this for Category enum

namespace ItemManagement.Models.ViewModels
{
    public class ItemFilterViewModel
    {
        // Property for filtering by Category (matches backend Category enum string values)
        public Category? Category { get; set; }

        // Property for filtering by Store (if you implement this filter later)
        public Guid? SelectedStoreId { get; set; }

        // Property for search by name (if you implement this filter later)
        public string? SearchQuery { get; set; }

        // List of available categories for a dropdown filter in the UI
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

        // List of available stores for a dropdown filter (will be populated from backend API later)
        public List<SelectListItem>? Stores { get; set; } = new List<SelectListItem>();
        public List<ItemViewModel>? Items { get; internal set; }
    }
}
