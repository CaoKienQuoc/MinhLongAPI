using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.Chat;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class ChatService : IChatService
    {
        private readonly IChatRoomRepository _roomRepo;
        private readonly IChatMessageRepository _msgRepo;

        public ChatService(IChatRoomRepository roomRepo, IChatMessageRepository msgRepo)
        {
            _roomRepo = roomRepo;
            _msgRepo = msgRepo;
        }

        public async Task<ChatRoom> CreateRoomAsync(string roomName, IEnumerable<Guid> memberIds)
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

            return await _roomRepo.AddAsync(room);
        }


        public async Task<IEnumerable<ChatRoomDto>> GetUserRoomsAsync(Guid userId)
        {
            // Lấy entity và include members + messages
            var rooms = await _roomRepo.GetForUserAsync(userId);

            // Map sang DTO
            var dtos = rooms.Select(r => new ChatRoomDto
            {
                ChatRoomId = r.ChatRoomId,
                RoomName = r.RoomName,
                CreatedAt = r.CreatedAt,
                MemberCount = r.Members.Count,
                MemberIds = r.Members.Select(m => m.UserId),
                LastMessage = r.Messages
                                    .OrderByDescending(m => m.Timestamp)
                                    .FirstOrDefault()
                                    ?.MessageText,
                LastTimestamp = r.Messages
                                    .OrderByDescending(m => m.Timestamp)
                                    .FirstOrDefault()
                                    ?.Timestamp
            });

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
                MemberIds = room.Members.Select(m => m.UserId),
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
                FileUrl = m.FileUrl,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead
            });
        }

        public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return false;
            // room.Members đã được include trong GetByIdAsync
            return room.Members.Any(m => m.UserId == userId);
        }

        public Task<ChatMessage> SaveMessageAsync(ChatMessage message) =>
            _msgRepo.AddAsync(message);
    }
}
