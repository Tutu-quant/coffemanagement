using Quản_lý_quán_cafe.Models.ViewModels.Account;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;

        public AccountService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool Success, string Message, int? UserId, int? RoleId, string? RoleName)> LoginAsync(LoginViewModel model)
        {
            // Authenticate user
            var user = await _userRepository.AuthenticateAsync(model.Username, model.Password);
            if (user == null)
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác", null, null, null);
            }

            // Get role from database
            var role = await _userRepository.GetRoleByIdAsync(user.RoleID);
            if (role == null)
            {
                return (false, "Vị trí không hợp lệ", null, null, null);
            }

            // Update last login time - Note: User entity không có LastLoginAt column
            await _userRepository.UpdateAsync(user);

            return (true, "Đăng nhập thành công", user.UserID, user.RoleID, role.RoleName);
        }

        public async Task LogoutAsync()
        {
            // Logout logic if needed
            await Task.CompletedTask;
        }
    }
}
