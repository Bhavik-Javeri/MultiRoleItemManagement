using System;
using System.ComponentModel.DataAnnotations; // Needed for validation attributes
using Microsoft.AspNetCore.Http; // Needed for IFormFile
using System.Text.Json.Serialization;

namespace ItemManagement.Models.ViewModels
{
    // This comprehensive ViewModel is designed for both displaying store details
    // and facilitating Create/Edit operations on the frontend for Stores.
    public class StoreViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Store Name is required.")]
        [StringLength(255, ErrorMessage = "Store Name cannot exceed 255 characters.")]
        [Display(Name = "Store Name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        // Location properties as strings for display
        [Display(Name = "Country")]
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "State")]
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [Display(Name = "City")]
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        // Location IDs for API communication
        [Required(ErrorMessage = "Country is required.")]
        [Display(Name = "Country")]
        [JsonPropertyName("countryId")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [Display(Name = "State")]
        [JsonPropertyName("stateId")]
        public int StateId { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [Display(Name = "City")]
        [JsonPropertyName("cityId")]
        public int CityId { get; set; }

        // Contact Information
        [Required(ErrorMessage = "Contact Number is required.")]
        [Display(Name = "Contact Number")]
        [Phone(ErrorMessage = "Invalid contact number format.")]
        [JsonPropertyName("contactNumber")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        // Operational Hours
        [Required(ErrorMessage = "Opening Hours is required.")]
        [Display(Name = "Opening Hours")]
        [DataType(DataType.Time)]
        [JsonPropertyName("openingHours")]
        public TimeSpan OpeningHours { get; set; }

        [Required(ErrorMessage = "Closing Hours is required.")]
        [Display(Name = "Closing Hours")]
        [DataType(DataType.Time)]
        [JsonPropertyName("closingHours")]
        public TimeSpan ClosingHours { get; set; }

        // Image handling
        [Display(Name = "Current Image")]
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Upload New Image")]
        public IFormFile? ImageFile { get; set; }

        // Status
        [Display(Name = "Status")]
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        // Audit Fields
        [Display(Name = "Creation Date")]
        [DataType(DataType.Date)]
        [JsonPropertyName("createDate")]
        public DateTime CreateDate { get; set; }

        [Display(Name = "Last Updated")]
        [DataType(DataType.Date)]
        [JsonPropertyName("updateDate")]
        public DateTime? UpdateDate { get; set; }

        // Admin-specific display field
        [Display(Name = "Store Admins")]
        [JsonPropertyName("userCount")]
        public int? UserCount { get; set; }

        // --- Mapping Method to Backend DTO (for Create/Update operations) ---
        // This static method converts this ViewModel into a DTO that your backend's Store API
        // would expect for creating or updating a store.
        public static object ToStoreDto(StoreViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            return new
            {
                id = viewModel.Id,
                name = viewModel.Name,
                address = viewModel.Address,
                countryId = viewModel.CountryId,
                stateId = viewModel.StateId,
                cityId = viewModel.CityId,
                contactNumber = viewModel.ContactNumber,
                email = viewModel.Email,
                openingHours = viewModel.OpeningHours,
                closingHours = viewModel.ClosingHours,
                imageUrl = viewModel.ImageUrl,
                isActive = viewModel.IsActive
            };
        }
    }
}
