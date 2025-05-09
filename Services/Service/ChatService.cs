using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class ChatService : IChatService
    {
        private readonly IChatMessageRepository _repository;

        public ChatService(IChatMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ChatMessageDto>> GetAllMessagesAsync()
        {
            return await _repository.GetAllMessagesAsync();
        }


        public async Task SaveMessageAsync(ChatMessage message)
        {
            try
            {
                await _repository.AddMessageAsync(message);
                await _repository.SaveChangesAsync();
                Console.WriteLine($"✅ Message saved: {message.ChatMessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChatService.SaveMessageAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(Guid user1, Guid user2)
        {
            return await _repository.GetMessagesAsync(user1, user2);
        }
    }
}
