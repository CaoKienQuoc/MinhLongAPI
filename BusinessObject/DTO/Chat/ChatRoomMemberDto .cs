using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Chat
{
    public class ChatRoomMemberDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }
}
