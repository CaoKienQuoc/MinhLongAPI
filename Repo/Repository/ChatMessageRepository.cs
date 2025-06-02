using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly MinhLongDbContext _context;
        public ChatMessageRepository(MinhLongDbContext ctx) => _context = ctx;

        public async Task<ChatMessage> AddAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        public Task<List<ChatMessage>> GetByRoomAsync(Guid roomId, int skip = 0, int take = 200) =>
        _context.ChatMessages
            .Where(m => m.ChatRoomId == roomId)
            .Include(m => m.Sender)
            .Include(m => m.Images)
            .OrderBy(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        public async Task<List<ChatMessage>> GetUnreadMessages(Guid chatRoomId, Guid userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatRoomId == chatRoomId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();
        }


    }
}
