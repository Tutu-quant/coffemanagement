using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Realtime;

public class AppStateHub(
    ApplicationDbContext context,
    IStaffNotificationConnectionRegistry staffConnections) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var session = Context.GetHttpContext()?.Session;
        var userId = session?.GetInt32("UserId");
        if (!userId.HasValue)
        {
            Context.Abort();
            return;
        }

        var user = await context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Employee)
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.UserID == userId.Value, Context.ConnectionAborted);
        var roleName = user?.Role?.RoleName ?? string.Empty;
        var staffAccountInvalid = roleName is "Admin" or "Cashier"
            && (user?.Employee is null || user.Employee.IsDeleted || !user.Employee.IsActive);
        var customerAccountInvalid = roleName == "Customer"
            && (user?.Customer is null || user.Customer.IsDeleted || !user.Customer.IsActive);

        if (user is null || user.IsDeleted || !user.IsActive || user.Role is null || user.Role.IsDeleted
            || staffAccountInvalid || customerAccountInvalid)
        {
            Context.Abort();
            return;
        }

        if (roleName is "Admin" or "Cashier")
        {
            staffConnections.Register(Context.ConnectionId, user.UserID);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        staffConnections.Unregister(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
