using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Services.IService;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MLHR.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ Client connected. UserId: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }

        public async Task SendMessage(Guid senderId, Guid receiverId, string message)
        {
            try
            {
                Console.WriteLine($"🟢 SendMessage called. Sender: {senderId}, Receiver: {receiverId}, Message: {message}");

                if (senderId == Guid.Empty || receiverId == Guid.Empty)
                    throw new HubException("❌ Sender or Receiver ID is empty");

                if (string.IsNullOrWhiteSpace(message))
                    throw new HubException("❌ Message cannot be empty");

                // ✅ Tạo ChatMessage
                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    MessageText = message,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                // ✅ Lưu vào DB
                await _chatService.SaveMessageAsync(chatMessage);

                // ✅ Gửi tới người nhận và chính người gửi
                await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
                {
                    senderId,
                    message
                });

                await Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", new
                {
                    senderId,
                    message
                });

                Console.WriteLine($"✅ Message sent to receiver {receiverId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SendMessage: {ex.Message}");
                throw;
            }
        }

    }


}
