using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace ItemManagement.Models.ViewModels
{
    public class UserProfileViewModel
    {
        // Not editable by user, but useful for display or internal tracking
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public bool IsActive { get; set; }

        // Editable fields
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string FName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string LName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        public string MobileNumber { get; set; } = string.Empty;

        // Current profile image URL/path for display
        public string? CurrentProfileImageUrl { get; set; }

        // For new profile image upload
        public IFormFile? NewProfileImage { get; set; }
    }
}
