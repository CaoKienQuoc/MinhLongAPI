using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Services.IService;  // namespace của IChatService

namespace MLHR.Hubs
{
    public class ChatHub : Hub
    {
        // Map từ UserId → ConnectionId để có thể gửi message trực tiếp nếu cần
        private static readonly ConcurrentDictionary<Guid, string> UserConnections
            = new ConcurrentDictionary<Guid, string>();

        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Khi client connect lên hub
        public override Task OnConnectedAsync()
        {
            if (Guid.TryParse(Context.UserIdentifier, out var userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                Console.WriteLine($"✅ Connected: User={userId}, ConnId={Context.ConnectionId}");
            }
            return base.OnConnectedAsync();
        }

        // Khi client disconnect khỏi hub
        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (Guid.TryParse(Context.UserIdentifier, out var userId))
            {
                UserConnections.TryRemove(userId, out _);
                Console.WriteLine($"❌ Disconnected: User={userId}");
            }
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client gọi để join vào một room cụ thể.
        /// FE sẽ invoke: connection.invoke("JoinRoom", roomId);
        /// </summary>
        public Task JoinRoom(Guid roomId)
        {
            var groupName = roomId.ToString();
            Console.WriteLine($"🔗 Connection {Context.ConnectionId} joining room {groupName}");
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Client gọi để gửi tin nhắn vào room.
        /// FE invoke: connection.invoke("SendMessage", roomId, senderId, messageText, fileUrl);
        /// </summary>
        public async Task SendMessage(Guid roomId, Guid senderId, string message, string fileUrl = null)
        {
            // Validate input
            if (roomId == Guid.Empty)
                throw new HubException("RoomId is required.");
            if (senderId == Guid.Empty)
                throw new HubException("SenderId is required.");
            /*if (receiverId == Guid.Empty)
                throw new HubException("SenderId is required.");*/
            if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(fileUrl))
                throw new HubException("Either message text or fileUrl must be provided.");

            // 1) Lưu tin nhắn xuống DB
            var chatMessage = new ChatMessage
            {
                ChatRoomId = roomId,
                SenderId = senderId,
                //ReceiverId = receiverId,
                MessageText = message,
                FileUrl = fileUrl,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            var saved = await _chatService.SaveMessageAsync(chatMessage);

            // 2) Broadcast cho tất cả client đang trong room
            var payload = new
            {
                saved.ChatMessageId,
                saved.ChatRoomId,
                saved.SenderId,
                saved.MessageText,
                saved.FileUrl,
                saved.Timestamp
            };
            await Clients.Group(roomId.ToString())
                         .SendAsync("ReceiveMessage", payload);
        }
    }
}
