using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Chat
{
    // DTOs/ChatRoomDto.cs
    public class ChatRoomDto
    {
        public Guid ChatRoomId { get; set; }
        public string RoomName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
      //  public IEnumerable<Guid> MemberIds { get; set; }

        // Thông tin message cuối cùng
        public string LastMessage { get; set; }
        public DateTime? LastTimestamp { get; set; }

        public List<ChatRoomMemberDto> Members { get; set; } = new List<ChatRoomMemberDto>();
    }

}
