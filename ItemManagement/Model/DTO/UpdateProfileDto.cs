using System.ComponentModel.DataAnnotations;
using System; // For Guid?
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model.DTO
{
    public class UpdateProfileDto
    {
        // It's good practice to include the ID in the DTO body for PUT requests,
        // even if it's also in the route, for robustness.
        public Guid Id { get; set; } // Added for completeness, if API expects it in body

        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string? FName { get; set; }

        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string? LName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        public string? MobileNumber { get; set; }

        // Profile image path/URL. No need for StringLength here if it's already in the model.
        public string? Profile { get; set; }

        // --- NEW PROPERTIES ADDED ---
        public string? Role { get; set; } // Added for updating user roles
        public bool IsActive { get; set; } // Added for updating user active status
        public Guid? StoreId { get; set; } // Added for assigning/unassigning stores for StoreAdmins
    }
}
