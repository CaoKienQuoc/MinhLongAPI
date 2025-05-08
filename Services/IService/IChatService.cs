using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IChatService
    {
        Task SaveMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetChatHistoryAsync(Guid user1, Guid user2);
    }
}
