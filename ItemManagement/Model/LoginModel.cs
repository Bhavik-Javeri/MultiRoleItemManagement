using System.ComponentModel.DataAnnotations;

namespace ItemManagement.Model
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)] // Provides hints for UI
        public string Password { get; set; } = string.Empty;
    }
}
