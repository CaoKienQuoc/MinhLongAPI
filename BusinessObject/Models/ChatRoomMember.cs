using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    // Models/ChatRoomMember.cs
    public class ChatRoomMember
    {
        public Guid ChatRoomMemberId { get; set; } = Guid.NewGuid();
        public Guid ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public string Role { get; set; }   // e.g. "Admin", "Member"
    }
}
