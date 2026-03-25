using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GodivaShop.Web.Hubs;

public class OrderHub : Hub
{
    // Admin join group để nhận thông báo
    [Authorize(Roles = "Admin,Staff")]
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
    }
}