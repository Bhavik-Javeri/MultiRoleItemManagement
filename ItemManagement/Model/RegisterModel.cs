using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model
{
    public class RegisterModel
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
        public string? Password { get; set; } = string.Empty; // Not required at registration

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        public string MobileNumber { get; set; } = string.Empty;

        
        public string? Profile { get; set; } // Nullable if optional

        // Crucial for specifying the role during registration (used carefully in AuthController)
        [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
        public string Role { get; set; } = "User"; // Default to "User" if not provided in the request
    }
}
