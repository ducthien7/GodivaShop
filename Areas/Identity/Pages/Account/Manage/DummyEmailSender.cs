using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace GodivaShop.Web // Đổi lại thành namespace chuẩn của bạn nếu khác
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Không làm gì cả, giả vờ như đã gửi email thành công để lừa hệ thống
            return Task.CompletedTask;
        }
    }
}