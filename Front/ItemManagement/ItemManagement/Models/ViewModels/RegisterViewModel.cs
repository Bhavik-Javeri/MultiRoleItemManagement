
// ItemManagement.Models.ViewModels.RegisterViewModel.cs
using Microsoft.AspNetCore.Http; // For IFormFile
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Models.ViewModels
{
    public class RegisterViewModel
    {
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

        //[Required(ErrorMessage = "Password is required.")]
        public string? Password { get; set; } = string.Empty; // Not required at registration

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        public string MobileNumber { get; set; } = string.Empty;

        // IFormFile for file uploads
        //public IFormFile? Profile { get; set; }

        // Added for role selection during registration (for testing; typically admin-managed)
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "User"; // Default to "User" if not specified
    }
}
