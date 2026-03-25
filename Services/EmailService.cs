using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GodivaShop.Web.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

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
        try
        {
            // If email credentials are not configured, skip sending
            var username = _config["Email:Username"];
            var password = _config["Email:Password"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return;

            // Use STARTTLS on port 587 which is recommended for Gmail
            await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch
        {
            // Swallow exceptions so order flow isn't blocked by email failures.
            // For production add ILogger and proper error handling.
        }
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
        try
        {
            await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log and swallow exceptions so order placement isn't blocked by email failures
            // In a real app inject ILogger<EmailService> and log here. For now just ignore.
        }
    }
}