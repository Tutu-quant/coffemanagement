using Quản_lý_quán_cafe.Models.ViewModels.Account;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IAccountService
    {
        Task<(bool Success, string Message, int? UserId, int? RoleId, string? RoleName)> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
    }
}
