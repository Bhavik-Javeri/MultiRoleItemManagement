using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ItemManagement.Model // Ensure this namespace matches your backend project's Model folder
{
    public class Store
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Address { get; set; } // Added

        [Required]
        [ForeignKey("Country")] // Ensure this matches your actual foreign key setup
        public int CountryId { get; set; }

        [Required]
        [ForeignKey("State")] // Ensure this matches your actual foreign key setup
        public int StateId { get; set; }

        [Required]
        [ForeignKey("City")] // Ensure this matches your actual foreign key setup
        public int CityId { get; set; }

        [StringLength(20)] // Appropriate length for phone number
        public string ContactNumber { get; set; } = null!; // Added, assuming it's required

        [StringLength(255)] // Appropriate length for email
        public string Email { get; set; } = null!; // Added, assuming it's required

        [Required]
        public TimeSpan OpeningHours { get; set; } // Added

        [Required]
        public TimeSpan ClosingHours { get; set; } // Added

        public string? Image { get; set; } // Your previous model had 'Image', keeping consistent

        public bool IsActive { get; set; } = true; // Added, with default value

        public DateTime CreateDate { get; set; } = DateTime.UtcNow; // Added, with default UTC time

        public DateTime? UpdateDate { get; set; } // Added, nullable

        // UserCount is typically calculated or derived, not stored directly in the Store table
        // But if you must store it, you'd add:
        // [NotMapped] // To indicate it's not a direct column, if it's calculated in code
        // public int UserCount { get; set; } // This is NOT typically a database column

        // Relationships (ensure these are correctly configured in your DbContext as well)
        public virtual ICollection<User>? Users { get; set; } // Corrected to use 'Users' directly for count
        public virtual ICollection<Item>? Items { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }

        // Navigation properties (ensure these match your actual entity names)
        public virtual Country Country { get; set; } = null!;
        public virtual State State { get; set; } = null!;
        public virtual City City { get; set; } = null!;
    }

  
}
