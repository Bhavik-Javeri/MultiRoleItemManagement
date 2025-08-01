using System;
using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model.DTO
{
    public class SetPasswordDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; }
    }
} 