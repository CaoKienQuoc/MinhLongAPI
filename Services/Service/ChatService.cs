using BusinessObject.DTO;
using BusinessObject.DTO.Chat;
using BusinessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class ChatService : IChatService
    {
        private readonly IChatRoomRepository _roomRepo;
        private readonly IChatMessageRepository _msgRepo;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hub;

        public ChatService(IChatRoomRepository roomRepo, IChatMessageRepository msgRepo, INotificationRepository notificationRepository, IHubContext<NotificationHub> hub)
        {
            _roomRepo = roomRepo;
            _msgRepo = msgRepo;
            _notificationRepository = notificationRepository;
            _hub = hub;
        }

        public DateTime GetVietnamTime()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }

        /*public async Task<ChatRoom> CreateRoomAsync(Guid? roomName, IEnumerable<Guid> memberIds)
        {
            // 1) Kiểm tra room đã tồn tại chưa (ví dụ cặp 2 thành viên)
            //    Giả sử bạn chỉ hỗ trợ 1-1 chat, bạn có thể tìm room có đúng 2 members đó
            var existing = await _roomRepo.FindByMembersAsync(memberIds);
            if (existing != null)
                return existing;

            // 2) Nếu chưa có, tạo mới
            var room = new ChatRoom { RoomName = roomName };
            var creatorId = memberIds.First();
            room.Members = memberIds.Select(uid => new ChatRoomMember
            {
                UserId = uid,
                ChatRoom = room,
                Role = uid == creatorId ? "Admin" : "Member",
                JoinedAt = DateTime.UtcNow
            }).ToList();

            *//*return await _roomRepo.AddAsync(room);*//*

            // 3. Lưu phòng vào database
            var createdRoom = await _roomRepo.AddAsync(room);

            // 4. Gửi tin nhắn chào mừng từ hệ thống
            var welcomeMessage = new ChatMessage
            {
                ChatRoomId = createdRoom.ChatRoomId,
                SenderId = new Guid("00000000-0000-0000-0000-000000000001"), // ID của hệ thống
                MessageText = "Cảm ơn bạn đã lựa chọn mua sắm ở Minh Long, nếu có thắc mắc cần giải đáp gì hãy nhắn tin cho chúng tôi, đội ngũ nhân viên sẽ giúp đỡ bạn!",
                Timestamp = DateTime.UtcNow
            };

            await _msgRepo.AddAsync(welcomeMessage);

            return createdRoom;

        }*/

        public async Task<ChatRoom> CreateRoomAsync(Guid? roomName, IEnumerable<Guid> memberIds)
        {
            var existing = await _roomRepo.FindByMembersAsync(memberIds);
            if (existing != null)
                return existing;

            var room = new ChatRoom { RoomName = roomName };
            var creatorId = memberIds.First();
            room.Members = memberIds.Select(uid => new ChatRoomMember
            {
                UserId = uid,
                ChatRoom = room,
                Role = uid == creatorId ? "Admin" : "Member",
                JoinedAt = GetVietnamTime()
            }).ToList();

            var createdRoom = await _roomRepo.AddAsync(room);

            // Gửi tin nhắn chào mừng từ Sale tới Agency
            var saleId = memberIds.FirstOrDefault(id => id != creatorId);
            var welcomeMessage = new ChatMessage
            {
                ChatRoomId = createdRoom.ChatRoomId,
                SenderId = saleId,
                MessageText = "Cảm ơn bạn đã lựa chọn Minh Long để mua sắm. Hãy liên hệ cho chnếu cần hỗ trợ!",
                Timestamp = GetVietnamTime()
            };
            await _msgRepo.AddAsync(welcomeMessage);
            await _msgRepo.SaveChangesAsync();

            // Gửi SignalR đến tất cả thành viên trong phòng với payload thật
            var payload = new
            {
                ChatMessageId = welcomeMessage.ChatMessageId,
                ChatRoomId = createdRoom.ChatRoomId,
                SenderId = welcomeMessage.SenderId,
                MessageText = welcomeMessage.MessageText,
                Timestamp = welcomeMessage.Timestamp,
                Images = new List<string>() // nếu sau này có ảnh thì truyền
            };

            foreach (var memberId in memberIds)
            {
                await _hub.Clients.User(memberId.ToString()).SendAsync("ReceiveMessageChatRoom", payload);
            }

            // Gửi Notification cho creator (nếu muốn giữ logic thông báo ngoài luồng SignalR)
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = creatorId,
                Title = "Tin nhắn mới",
                Message = "Bạn vừa nhận được tin nhắn mới.",
                Url = $"/chat/room/{createdRoom.ChatRoomId}",
                CreatedAt = GetVietnamTime()
            });
            await _notificationRepository.SaveChangesAsync();

            return createdRoom;
        }



        /*public async Task<IEnumerable<ChatRoomDto>> GetUserRoomsAsync(Guid userId)
        {
            // Lấy entity và include members + messages
            var rooms = await _roomRepo.GetForUserAsync(userId);

            // Map sang DTO
            var dtos = rooms.Select(r =>
            {
                var lastMessage = r.Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                return new ChatRoomDto
                {
                    ChatRoomId = r.ChatRoomId,
                    RoomName = r.RoomName,
                    CreatedAt = r.CreatedAt,
                    MemberCount = r.Members.Count,
                    Members = r.Members.Select(m => new ChatRoomMemberDto
                    {
                        UserId = m.UserId,
                        Name = m.User.Employee != null ? m.User.Employee.FullName
                              : m.User.AgencyAccount != null ? m.User.AgencyAccount.AgencyName
                              : m.User.Username
                    }).ToList(),
                    LastMessage = lastMessage?.MessageText,
                    LastTimestamp = lastMessage?.Timestamp ?? DateTime.MinValue,
                    LastUserId =  lastMessage.SenderId,
                    LastUserName = lastMessage.Sender?.Employee != null
                                     ? lastMessage.Sender.Employee.FullName
                                        : lastMessage.Sender?.AgencyAccount != null
                                        ? lastMessage.Sender.AgencyAccount.AgencyName
                                        : lastMessage.Sender?.Username
                };
            })
                    .OrderByDescending(r => r.LastTimestamp) // Sắp xếp theo thời gian tin nhắn mới nhất
                    .ToList();

            return dtos;
        }*/

        public async Task<IEnumerable<ChatRoomDto>> GetUserRoomsAsync(Guid userId)
        {
            // Lấy danh sách phòng có liên quan đến người dùng
            var rooms = await _roomRepo.GetForUserAsync(userId);

            // Chuyển sang DTO
            var dtos = rooms.Select(r =>
            {
                var lastMessage = r.Messages
                                   .OrderByDescending(m => m.Timestamp)
                                   .FirstOrDefault();

                return new ChatRoomDto
                {
                    ChatRoomId = r.ChatRoomId,
                    RoomName = r.RoomName,
                    CreatedAt = r.CreatedAt,
                    MemberCount = r.Members.Count,
                    Members = r.Members.Select(m => new ChatRoomMemberDto
                    {
                        UserId = m.UserId,
                        Name = m.User.Employee?.FullName
                               ?? m.User.AgencyAccount?.AgencyName
                               ?? m.User.Username
                    }).ToList(),

                    LastMessage = lastMessage?.MessageText ?? "Chưa có tin nhắn nào",
                    LastTimestamp = lastMessage?.Timestamp ?? DateTime.MinValue,
                    LastUserId = (Guid)(lastMessage?.SenderId ?? Guid.Empty),
                    LastUserName = lastMessage?.Sender?.Employee?.FullName
                                   ?? lastMessage?.Sender?.AgencyAccount?.AgencyName
                                   
                };
            })
            .OrderByDescending(r => r.LastTimestamp) // Ưu tiên phòng có hoạt động gần nhất
            .ToList();

            return dtos;
        }


        public async Task<ChatRoomDto> GetRoomByIdAsync(Guid roomId)
        {
            // Lấy entity room kèm members và messages
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return null;

            // Map ra DTO
            var last = room.Messages
                           .OrderByDescending(m => m.Timestamp)
                           .FirstOrDefault();

            return new ChatRoomDto
            {
                ChatRoomId = room.ChatRoomId,
                RoomName = room.RoomName,
                CreatedAt = room.CreatedAt,
                MemberCount = room.Members.Count,
                Members = room.Members.Select(m => new ChatRoomMemberDto
                {
                    UserId = m.UserId,
                    Name = m.User.Employee != null ? m.User.Employee.FullName
                          : m.User.AgencyAccount != null ? m.User.AgencyAccount.AgencyName
                          : m.User.Username
                }).ToList(),
                LastMessage = last?.MessageText,
                LastTimestamp = last?.Timestamp
            };
        }




        public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid roomId, int skip = 0, int take = 50)
        {
            var messages = await _msgRepo.GetByRoomAsync(roomId, skip, take);

            // Map to DTO
            return messages.Select(m => new ChatMessageDto
            {
                ChatMessageId = m.ChatMessageId,
                ChatRoomId = m.ChatRoomId.Value,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Username,   // hoặc m.Sender.Email tuỳ UI
                MessageText = m.MessageText,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,

                // ✅ Map danh sách ImageUrl
                ImageUrls = m.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
            });
        }

        public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return false;
            // room.Members đã được include trong GetByIdAsync
            return room.Members.Any(m => m.UserId == userId);
        }

        public async Task MarkMessagesAsReadAsync(Guid chatRoomId, Guid userId)
        {
            var unreadMessages = await _msgRepo.GetUnreadMessages(chatRoomId, userId);

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            await _msgRepo.SaveChangesAsync();
        }


        public Task<ChatMessage> SaveMessageAsync(ChatMessage message) =>
            _msgRepo.AddAsync(message);
    }
}
