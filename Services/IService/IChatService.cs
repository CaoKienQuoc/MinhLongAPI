using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.DTO.Chat;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IChatService
    {
        Task<ChatRoom> CreateRoomAsync(Guid? roomName, IEnumerable<Guid> memberIds);
        Task<IEnumerable<ChatRoomDto>> GetUserRoomsAsync(Guid userId);
        // IChatService.cs
        Task<ChatRoomDto> GetRoomByIdAsync(Guid roomId);

        Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid roomId, int skip = 0, int take = 200);
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);

        Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);
        Task MarkMessagesAsReadAsync(Guid chatRoomId, Guid userId);

        Task<int> CountUnreadMessagesAsync(Guid userId);
    }
}
