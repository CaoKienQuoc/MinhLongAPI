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

        public ChatMessageRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessageDto>> GetAllMessagesAsync()
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Select(m => new ChatMessageDto
                {
                    ChatMessageId = m.ChatMessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    SenderName = m.Sender.AgencyAccount != null ? m.Sender.AgencyAccount.AgencyName : m.Sender.Employee.FullName,
                    ReceiverName = m.Receiver.AgencyAccount != null ? m.Receiver.AgencyAccount.AgencyName : m.Receiver.Employee.FullName,
                    MessageText = m.MessageText,
                    FileUrl = m.FileUrl,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead
                })
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ChatMessageDto>> GetMessagesBySenderAsync(Guid senderId)
        {
            return await _context.ChatMessages
                .Where(m => m.SenderId == senderId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Select(m => new ChatMessageDto
                {
                    ChatMessageId = m.ChatMessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    SenderName = m.Sender.AgencyAccount != null ? m.Sender.AgencyAccount.AgencyName : m.Sender.Employee.FullName,
                    ReceiverName = m.Receiver.AgencyAccount != null ? m.Receiver.AgencyAccount.AgencyName : m.Receiver.Employee.FullName,
                    MessageText = m.MessageText,
                    FileUrl = m.FileUrl,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead
                })
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ChatMessageDto>> GetMessagesByReceiverAsync(Guid receiverId)
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == receiverId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Select(m => new ChatMessageDto
                {
                    ChatMessageId = m.ChatMessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    SenderName = m.Sender.AgencyAccount != null ? m.Sender.AgencyAccount.AgencyName : m.Sender.Employee.FullName,
                    ReceiverName = m.Receiver.AgencyAccount != null ? m.Receiver.AgencyAccount.AgencyName : m.Receiver.Employee.FullName,
                    MessageText = m.MessageText,
                    FileUrl = m.FileUrl,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead
                })
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(Guid user1, Guid user2)
        {
            return await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == user1 && m.ReceiverId == user2) ||
                    (m.SenderId == user2 && m.ReceiverId == user1))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task DeleteOldMessagesAsync(DateTime olderThan)
        {
            var oldMessages = _context.ChatMessages.Where(m => m.Timestamp < olderThan);
            _context.ChatMessages.RemoveRange(oldMessages);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
