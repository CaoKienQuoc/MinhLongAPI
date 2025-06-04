using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IChatRoomRepository
    {
        Task<ChatRoom> AddAsync(ChatRoom room);
        Task<ChatRoom> GetByIdAsync(Guid roomId);
        Task<List<ChatRoom>> GetForUserAsync(Guid userId);
        Task<ChatRoomMember> GetRoomMemberAsync(Guid roomId, Guid userId);
        Task<ChatRoom> FindByMembersAsync(IEnumerable<Guid> memberIds);
    }
}
