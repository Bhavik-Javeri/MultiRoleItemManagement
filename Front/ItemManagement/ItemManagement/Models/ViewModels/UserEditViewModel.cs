using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // For IFormFile
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem

namespace ItemManagement.Models.ViewModels // THIS NAMESPACE IS CRITICAL
{
    public class UserEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        [Display(Name = "First Name")]
        public string FName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        [Display(Name = "Last Name")]
        public string LName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        // Profile image is optional; user can be created/edited without uploading a profile image.
        [Display(Name = "Current Profile Image")]
        public string? CurrentProfileImageUrl { get; set; } // Nullable, not required

        [Display(Name = "Upload New Profile Image")]
        public IFormFile? NewProfileImage { get; set; } // Nullable, not required

        [Required(ErrorMessage = "Role is required.")]
        [Display(Name = "User Role")]
        public string? Role { get; set; } = "User";
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Assigned Store (for Store Admin)")]
        public Guid? StoreId { get; set; }

        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "SuperAdmin", Text = "Super Admin" },
            new SelectListItem { Value = "StoreAdmin", Text = "Store Admin" },
            new SelectListItem { Value = "User", Text = "User" }
        };

        public List<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
        public string? StoreName { get; internal set; }
    }
}
