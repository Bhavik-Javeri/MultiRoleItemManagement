using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model.DTO
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        public string FName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        public string LName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be 10 digits")]
        public string MobileNumber { get; set; }

        [StringLength(200, ErrorMessage = "Profile URL cannot be longer than 200 characters")]
        public string? Profile { get; set; }
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true; // NEW: Added IsActive property, defaulting to true
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        public Guid? StoreId { get; set; }
    }
} 