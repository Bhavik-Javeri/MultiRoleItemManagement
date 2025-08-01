using System;
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Models.ViewModels // THIS NAMESPACE IS CRITICAL
{
    public class UserListViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "First Name")]
        public string FName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; }

        [Display(Name = "Creation Date")]
        [DataType(DataType.Date)]
        public DateTime CreateDate { get; set; }

        [Display(Name = "Last Updated")]
        [DataType(DataType.Date)]
        public DateTime? UpdateDate { get; set; }

        [Display(Name = "Profile Image")]
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Assigned Store")]
        public Guid? StoreId { get; set; }

        [Display(Name = "Store Name")]
        public string? StoreName { get; set; }
    }
}
