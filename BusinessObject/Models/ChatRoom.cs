using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ChatRoom
    {
        public Guid ChatRoomId { get; set; } = Guid.NewGuid();
        public string RoomName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatRoomMember> Members { get; set; }
        public ICollection<ChatMessage> Messages { get; set; }
    }

}
