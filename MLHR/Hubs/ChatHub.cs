using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Services.IService;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MLHR.Hubs
{
    public class ChatHub : Hub
    {
        // Map UserId -> ConnectionId
        private static readonly ConcurrentDictionary<Guid, string> UserConnections
            = new ConcurrentDictionary<Guid, string>();

        private readonly IChatService _chatService;
        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override Task OnConnectedAsync()
        {
            var userIdStr = Context.UserIdentifier;
            if (Guid.TryParse(userIdStr, out var userGuid))
            {
                UserConnections[userGuid] = Context.ConnectionId;
                Console.WriteLine($"✅ Connected: UserId={userGuid}, ConnId={Context.ConnectionId}");
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userIdStr = Context.UserIdentifier;
            if (Guid.TryParse(userIdStr, out var userGuid))
            {
                UserConnections.TryRemove(userGuid, out _);
                Console.WriteLine($"❌ Disconnected: UserId={userGuid}");
            }
            return base.OnDisconnectedAsync(exception);
        }

        // Thêm method để client join nhóm
        public Task JoinGroup(string groupName)
        {
            Console.WriteLine($"🔗 {Context.ConnectionId} joining group {groupName}");
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Gửi tin nhắn đến đúng group, đồng thời vẫn log & lưu DB
        public async Task SendMessageToGroup(string groupName, Guid senderId, Guid receiverId, string message)
        {
            Console.WriteLine($"🟢 SendMessageToGroup: grp={groupName}, sender={senderId}, recv={receiverId}, msg={message}");

            if (senderId == Guid.Empty || receiverId == Guid.Empty)
                throw new HubException("Sender or Receiver ID is empty");
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty");

            // Lưu message vào DB
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageText = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            await _chatService.SaveMessageAsync(chatMessage);

            // Gửi đến tất cả connection trong group (thường chỉ có 2: sender+receiver)
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                senderId,
                message
            });
        }
    }
}
