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
        Task<ChatRoom> CreateRoomAsync(string roomName, IEnumerable<Guid> memberIds);
        Task<IEnumerable<ChatRoomDto>> GetUserRoomsAsync(Guid userId);
        // IChatService.cs
        Task<ChatRoomDto> GetRoomByIdAsync(Guid roomId);

        Task<List<ChatMessage>> GetRoomMessagesAsync(Guid roomId, int skip = 0, int take = 50);
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);
    }
}
