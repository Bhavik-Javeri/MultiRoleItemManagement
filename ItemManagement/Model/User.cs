using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItemManagement.Model
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

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

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(500, ErrorMessage = "Password hash cannot exceed 500 characters.")] // This will store the hashed password
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [StringLength(20, ErrorMessage = "Mobile Number cannot exceed 20 characters.")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
        public string Role { get; set; } = "User"; // Default role upon creation

        [Required]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdateDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [StringLength(500)] // Assuming this stores a path or URL
        public string? Profile { get; set; } // Nullable if profile picture is optional

        // Foreign key to Store (nullable for SuperAdmin or plain Users)
        public Guid? StoreId { get; set; }

        [ForeignKey("StoreId")]
        public Store? Store { get; set; } // Navigation property

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        [StringLength(500)] // Used for refresh tokens or password reset tokens if implemented
        public string? Token { get; set; }

        // Navigation properties for relationships
        public Cart? Cart { get; set; } // One-to-one or one-to-many depending on your Cart model
        public ICollection<Order>? Orders { get; set; } // One-to-many
    }
}
