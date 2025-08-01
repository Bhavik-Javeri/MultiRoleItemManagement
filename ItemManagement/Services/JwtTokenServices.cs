using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration; // Required to access IConfiguration

namespace ItemManagement.Services
{
    public class JwtTokenServices
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _durationInMinutes; // Field to store token duration

        public JwtTokenServices(IConfiguration configuration)
        {
            // Retrieve JWT settings from appsettings.json.
            // These lines will throw an ArgumentNullException if the keys are missing in appsettings.json,
            // which helps ensure critical configuration is present during startup.
            _key = configuration["JwtSettings:Key"] ?? throw new ArgumentNullException("JwtSettings:Key is not configured.");
            _issuer = configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException("JwtSettings:Issuer is not configured.");
            _audience = configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException("JwtSettings:Audience is not configured.");

            // Parse the token duration from appsettings.json. Default to 60 minutes if not found or invalid.
            if (!int.TryParse(configuration["JwtSettings:DurationInMinutes"], out _durationInMinutes))
            {
                _durationInMinutes = 60; // Default value
                Console.WriteLine("Warning: 'JwtSettings:DurationInMinutes' not found or invalid in appsettings.json. Defaulting to 60 minutes.");
            }
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for a given user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="role">The role of the user (e.g., "SuperAdmin", "StoreAdmin", "User").</param>
        /// <param name="storeId">The optional ID of the store the user is associated with. Nullable for users not tied to a specific store.</param>
        /// <returns>A signed JWT string.</returns>
        public string GenerateToken(Guid userId, string role, Guid? storeId, string fName, string lName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_key);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, role),
        new Claim("FName", fName),
        new Claim("LName", lName)
    };

            if (storeId.HasValue)
            {
                claims.Add(new Claim("StoreId", storeId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_durationInMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
