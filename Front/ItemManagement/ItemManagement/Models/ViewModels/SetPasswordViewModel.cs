using System;
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Models.ViewModels
{
    public class SetPasswordViewModel
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
} 