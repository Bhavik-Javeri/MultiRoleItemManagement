using ItemManagement.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Models
{
    public class Store
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        public int CountryId { get; set; }

        [Required]
        public int StateId { get; set; }

        [Required]
        public int CityId { get; set; }

        [Required]
        [Phone]
        public string ContactNumber { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public TimeSpan OpeningHours { get; set; }

        [Required]
        public TimeSpan ClosingHours { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdateDate { get; set; }

        public int UserCount { get; set; }

        // Relationships
        public virtual Country Country { get; set; }
        public virtual State State { get; set; }
        public virtual City City { get; set; }

        public static StoreViewModel ToViewModel(Store store)
        {
            return new StoreViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address,
                CountryId = store.CountryId,
                StateId = store.StateId,
                CityId = store.CityId,
                ContactNumber = store.ContactNumber,
                Email = store.Email,
                OpeningHours = store.OpeningHours,
                ClosingHours = store.ClosingHours,
                ImageUrl = store.ImageUrl,
                IsActive = store.IsActive,
                CreateDate = store.CreateDate,
                UpdateDate = store.UpdateDate,
                UserCount = store.UserCount
            };
        }
    }

    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
