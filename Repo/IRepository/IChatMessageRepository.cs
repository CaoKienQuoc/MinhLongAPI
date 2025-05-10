using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IChatMessageRepository
    {
        Task<ChatMessage> AddAsync(ChatMessage message);
        Task<List<ChatMessage>> GetByRoomAsync(Guid roomId, int skip = 0, int take = 50);
    }
}
