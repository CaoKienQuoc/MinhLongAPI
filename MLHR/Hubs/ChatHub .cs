using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Services.IService;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, string> UserConnections = new ConcurrentDictionary<Guid, string>();
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            // Lưu ConnectionId cho User
            UserConnections[userGuid] = Context.ConnectionId;
            Console.WriteLine($"✅ Client connected. UserId: {userGuid}, ConnectionId: {Context.ConnectionId}");
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            // Xóa ConnectionId khi User ngắt kết nối
            UserConnections.TryRemove(userGuid, out _);
            Console.WriteLine($"❌ Client disconnected. UserId: {userGuid}");
        }

        return base.OnDisconnectedAsync(exception);
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

            // Tạo ChatMessage
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageText = message,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            // Lưu vào DB
            await _chatService.SaveMessageAsync(chatMessage);

            // Gửi tin nhắn tới đúng người nhận
            if (UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", new
                {
                    senderId,
                    message
                });
                Console.WriteLine($"✅ Message sent to receiver {receiverId}");
            }

            // Gửi lại cho người gửi để xác nhận đã gửi
            if (UserConnections.TryGetValue(senderId, out var senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("ReceiveMessage", new
                {
                    senderId,
                    message
                });
                Console.WriteLine($"✅ Message sent to sender {senderId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in SendMessage: {ex.Message}");
            throw;
        }
    }
}
