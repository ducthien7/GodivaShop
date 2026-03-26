using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GodivaShop.Web.Hubs
{
    public class ChatHub : Hub
    {
        // Dùng ConcurrentDictionary để lưu trạng thái: true = Đang chat với Admin, false = Đang chat với Bot
        private static readonly ConcurrentDictionary<string, bool> _userSessions = new();

        public async Task SendMessageToAdmin(string customerName, string message)
        {
            var connId = Context.ConnectionId;
            var lowerMsg = message.ToLower();

            // 1. Khởi tạo trạng thái nếu khách vừa vào (Mặc định: Chat với Bot = false)
            if (!_userSessions.ContainsKey(connId))
            {
                _userSessions.TryAdd(connId, false);
            }

            bool isTalkingToAdmin = _userSessions[connId];

            // 2. RẼ NHÁNH: NẾU ĐANG CHAT VỚI BOT
            if (!isTalkingToAdmin)
            {
                // Kiểm tra xem khách có muốn gặp nhân viên thật không?
                if (lowerMsg.Contains("nhân viên") || lowerMsg.Contains("admin") || lowerMsg.Contains("hỗ trợ") || lowerMsg.Contains("tư vấn"))
                {
                    // Chuyển luồng sang Admin
                    _userSessions[connId] = true;

                    // Báo cho khách biết
                    await Clients.Client(connId).SendAsync("ReceiveReply", "Hệ thống", "⏳ Đang kết nối với nhân viên tư vấn. Vui lòng đợi trong giây lát...");

                    // Bắn thông báo và tin nhắn gốc cho toàn bộ Admin
                    await Clients.Group("Admins").SendAsync("ReceiveMessage", connId, customerName, $"⚠️ [YÊU CẦU HỖ TRỢ] Khách vừa nhắn: {message}");
                }
                else
                {
                    // Tự động trả lời theo từ khóa (Bạn có thể thêm bớt tùy ý)
                    string botReply = "Chào bạn! Mình là trợ lý ảo của Godiva 🤖. ";

                    if (lowerMsg.Contains("giá") || lowerMsg.Contains("nhiêu"))
                        botReply += "Giá sản phẩm được niêm yết trực tiếp trên website. Bạn có thể bấm vào từng món để xem nhé.";
                    else if (lowerMsg.Contains("ship") || lowerMsg.Contains("giao hàng"))
                        botReply += "Bên mình giao hàng toàn quốc. Phí ship sẽ được tính khi bạn thanh toán nha.";
                    else if (lowerMsg.Contains("địa chỉ") || lowerMsg.Contains("ở đâu"))
                        botReply += "Hiện tại hệ thống Godiva hoạt động chủ yếu qua nền tảng trực tuyến ạ.";
                    else
                        botReply += "Bạn cần hỏi cụ thể về món nào không? (Gõ **'gặp nhân viên'** nếu bạn cần tư vấn viên hỗ trợ trực tiếp nhé).";

                    // Phản hồi lại ngay lập tức cho khách
                    await Clients.Client(connId).SendAsync("ReceiveReply", "Godiva Bot 🤖", botReply);
                }
            }
            // 3. RẼ NHÁNH: NẾU ĐÃ CHUYỂN SANG ADMIN
            else
            {
                // Gửi thẳng tin nhắn của khách cho Admin, Bot không can thiệp nữa
                await Clients.Group("Admins").SendAsync("ReceiveMessage", connId, customerName, message);
            }
        }

        public async Task ReplyToCustomer(string connectionId, string message)
        {
            // Admin trả lời
            await Clients.Client(connectionId).SendAsync("ReceiveReply", "Admin Godiva 👩‍💻", message);
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        // Xóa phiên chat khi khách thoát trang web để dọn dẹp bộ nhớ
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _userSessions.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }
    }
}