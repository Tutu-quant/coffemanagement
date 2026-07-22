namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IUserService
    {
        Task<(bool Success, string Message)> CreateUserAsync(object userData);
        Task<(bool Success, string Message)> UpdateUserAsync(int id, object data);
    }
}
