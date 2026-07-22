using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class DashboardService : IDashboardService
    {
        public async Task<object> GetDashboardDataAsync()
        {
            try
            {
                return new { Success = true, Message = "Dashboard data retrieved" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}
