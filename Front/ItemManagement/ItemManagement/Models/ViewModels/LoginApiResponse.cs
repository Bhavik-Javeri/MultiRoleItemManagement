
// ItemManagement.Models.ViewModels.LoginApiResponse.cs (NEW FILE)
// This model will help deserialize the JSON response from your backend Login API
namespace ItemManagement.Models.ViewModels
{
    public class LoginApiResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        // Add any other properties your backend login response might return, e.g.:
        // public string UserId { get; set; }
        // public string Role { get; set; }
        // public Guid? StoreId { get; set; }
    }
}
