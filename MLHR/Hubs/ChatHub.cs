using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using BusinessObject.Models;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
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
        private readonly IImageService _imageService;

        public ChatHub(IChatService chatService, IImageService imageService)
        {
            _chatService = chatService;
            _imageService = imageService;
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
        public async Task SendMessage(Guid roomId, Guid senderId, string message, List<string> fileUrls = null, List<string> publicIds = null)
        {
            // Validate input
            if (roomId == Guid.Empty)
                throw new HubException("RoomId is required.");
            if (senderId == Guid.Empty)
                throw new HubException("SenderId is required.");
            if (string.IsNullOrWhiteSpace(message) && (fileUrls == null || !fileUrls.Any()))
                throw new HubException("Message or at least one image is required.");

            // 1) Lưu tin nhắn xuống DB
            var chatMessage = new ChatMessage
            {
                ChatRoomId = roomId,
                SenderId = senderId,
                //ReceiverId = receiverId,
                MessageText = message,
                Timestamp = GetVietnamTime(),
                IsRead = false
            };
            var saved = await _chatService.SaveMessageAsync(chatMessage);

            var uploadedImages = new List<ChatMessageImage>();

            if (fileUrls != null && fileUrls.Any())
            {
                for (int i = 0; i < fileUrls.Count; i++)
                {
                    var image = new ChatMessageImage
                    {
                        ChatMessageId = saved.ChatMessageId,
                        ImageUrl = fileUrls[i],
                        PublicId = publicIds != null && i < publicIds.Count ? publicIds[i] : null,
                        UploadedAt = GetVietnamTime()
                    };

                    await _imageService.SaveChatImageAsync(image);
                    uploadedImages.Add(image);
                }
            }

            // 3) Broadcast cho tất cả client đang trong room
            var payload = new
            {
                saved.ChatMessageId,
                saved.ChatRoomId,
                saved.SenderId,
                saved.MessageText,
                saved.Timestamp,
                Images = uploadedImages.Select(img => img.ImageUrl).ToList()
            };

            // 4) Gửi SignalR chỉ đến các thành viên khác người gửi
            var room = await _chatService.GetRoomByIdAsync(roomId);
            var receivers = room.Members
                                .Where(m => m.UserId != senderId)
                                .Select(m => m.UserId.ToString())
                                .ToList();

            foreach (var userId in receivers)
            {
                await Clients.User(userId).SendAsync("ReceiveMessage", payload);
            }

            /*await Clients.Group(roomId.ToString())
                         .SendAsync("ReceiveMessage", payload);*/
        }

        public DateTime GetVietnamTime()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }

    }
}
