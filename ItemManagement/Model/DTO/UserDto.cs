//using ItemManagement.Model;


namespace ItemManagement.Model.DTO


{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FName { get; set; } = null!;
        public string LName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } // Note: Passwords are usually not included in DTOs for security reasons
        public string MobileNumber { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsActive { get; set; }
        public string? Profile { get; set; }
        public Guid? StoreId { get; set; }
        public string? StoreName { get; set; }
        // public string? CreatedBy { get; set; }
        // public string? UpdatedBy { get; set; }
        public string? Token { get; set; }

        public static UserDto Mapping(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FName = user.FName,
                LName = user.LName,
                Email = user.Email,
                Password = user.Password, // Handle passwords securely
                MobileNumber = user.MobileNumber,
                Role = user.Role,
                CreateDate = user.CreateDate,
                UpdateDate = user.UpdateDate,
                IsActive = user.IsActive,
                Profile = user.Profile,
                StoreId = user?.StoreId ?? null,
                StoreName= user.Store?.Name,
                //CreatedBy = user.CreatedBy,
                //UpdatedBy = user.UpdatedBy,
                Token = user.Token
            };
        }

        public static List<UserDto> Mapping(List<User> users)
        {
            List<UserDto> lstUserDto = new List<UserDto>();
            foreach (User userObj in users)
            {
                lstUserDto.Add(Mapping(userObj));
            }
            return lstUserDto;
        }

        public static User Mapping(UserDto userDto)
        {
            return new User
            {
                Id = userDto.Id,
                FName = userDto.FName,
                LName = userDto.LName,
                Email = userDto.Email,
                Password = userDto.Password, // Handle passwords securely
                MobileNumber = userDto.MobileNumber,
                Role = userDto.Role,
                CreateDate = userDto.CreateDate,
                UpdateDate = userDto.UpdateDate,
                IsActive = userDto.IsActive,
                Profile = userDto.Profile,
                StoreId = userDto.StoreId,
                // CreatedBy = userDto.CreatedBy,
                //UpdatedBy = userDto.UpdatedBy,
                 Token = userDto.Token
            };
        }

        public static List<User> Mapping(List<UserDto> userDtos)
        {
            List<User> lstUsers = new List<User>();
            foreach (UserDto userDtoObj in userDtos)
            {
                lstUsers.Add(Mapping(userDtoObj));
            }
            return lstUsers;
        }
    }
}
