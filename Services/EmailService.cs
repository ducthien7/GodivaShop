using MailKit.Net.Smtp;
using MimeKit;

namespace GodivaShop.Web.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // ========================================================
    // 1. HÀM GỬI MÁY CHUNG (Dùng cho Welcome, Confirm, v.v...)
    // ========================================================
    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Godiva Shop", _config["Email:From"]));

        // Vì hàm chung có thể không có tên người nhận, mình dùng tạm email làm tên hiển thị
        message.To.Add(new MailboxAddress("", toEmail));

        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = htmlMessage
        };

        using var client = new SmtpClient();

        try
        {
            // Bắt chước y hệt cấu hình kết nối từ các hàm cũ của bạn
            await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), false);
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            // Bắt lỗi ra console để app không bị sập nếu sai mật khẩu mail
            Console.WriteLine($"[Lỗi gửi mail tới {toEmail}]: " + ex.Message);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    // ========================================================
    // 2. CÁC HÀM CŨ CỦA BẠN (Được giữ nguyên)
    // ========================================================
    public async Task SendOrderConfirmationAsync(string toEmail, string toName, int orderId, decimal total)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Godiva Shop", _config["Email:From"]));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Xác nhận đơn hàng #{orderId} - Godiva Shop";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family: Georgia, serif; max-width: 600px; margin: auto; color: #333;">
                <h2 style="color: #C1A35E;">Cảm ơn bạn đã đặt hàng tại Godiva!</h2>
                <p>Xin chào <strong>{toName}</strong>,</p>
                <p>Đơn hàng <strong>#{orderId}</strong> của bạn đã được tiếp nhận.</p>
                <p>Tổng tiền: <strong style="color:#C1A35E;">{total:N0} VNĐ</strong></p>
                <hr/>
                <p style="color:#888; font-size:12px;">Godiva Chocolate Vietnam</p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), false);
        await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendOrderStatusUpdateAsync(string toEmail, string toName, int orderId, string status)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Godiva Shop", _config["Email:From"]));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Cập nhật đơn hàng #{orderId}";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family: Georgia, serif; max-width: 600px; margin: auto;">
                <h2 style="color: #C1A35E;">Cập nhật trạng thái đơn hàng</h2>
                <p>Đơn hàng <strong>#{orderId}</strong> của bạn đã được cập nhật sang:
                   <strong style="color:#C1A35E;">{status}</strong></p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), false);
        await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}