using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Repo.IRepository
{
    public interface IChatMessageRepository
    {
        Task AddMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetMessagesAsync(Guid user1, Guid user2);
        Task DeleteOldMessagesAsync(DateTime olderThan);
        Task SaveChangesAsync();
    }
}
