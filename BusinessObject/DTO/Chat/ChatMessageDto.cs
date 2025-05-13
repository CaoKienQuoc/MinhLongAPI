using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Chat
{
    // DTOs/ChatMessageDto.cs
    public class ChatMessageDto
    {
        public Guid ChatMessageId { get; set; }
        public Guid ChatRoomId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }    // hiển thị username
        public string MessageText { get; set; }
        public List<string> ImageUrls { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

}
