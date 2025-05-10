using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly MinhLongDbContext _context;
        public ChatRoomRepository(MinhLongDbContext ctx) => _context = ctx;

        public async Task<ChatRoom> AddAsync(ChatRoom room)
        {
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public Task<ChatRoom> GetByIdAsync(Guid roomId) =>
            _context.ChatRooms
                .Where(r => r.ChatRoomId == roomId)
                .Include(r => r.Members)
                .Include(r => r.Messages)           // ← thêm dòng này
                .FirstOrDefaultAsync();


        public Task<List<ChatRoom>> GetForUserAsync(Guid userId) =>
                         _context.ChatRooms                // Bắt đầu từ ChatRooms
                            .Where(r =>                   // Lọc những room có member này
                                r.Members.Any(m => m.UserId == userId)
                                    )
                                .Include(r => r.Members)     // Include members trước khi ToListAsync
                                .Include(r=>r.Messages)
                                .ToListAsync();


        public async Task<ChatRoom> FindByMembersAsync(IEnumerable<Guid> memberIds)
        {
            var ids = memberIds.Distinct().OrderBy(x => x).ToArray();
            var count = ids.Length;

            // 1) Lấy candidate rooms sao cho số lượng members bằng count
            var candidates = await _context.ChatRooms
                .Where(r => r.Members.Count == count)
                .Include(r => r.Members)
                .ThenInclude(m => m.User)
                .ToListAsync();   // => đưa về bộ nhớ để so sánh chính xác

            // 2) Trong bộ nhớ, tìm room có đúng tập userIds
            foreach (var room in candidates)
            {
                var memberIdsInRoom = room.Members
                    .Select(m => m.UserId)
                    .OrderBy(x => x)
                    .ToArray();

                if (memberIdsInRoom.SequenceEqual(ids))
                    return room;
            }

            return null;
        }

    }
}
