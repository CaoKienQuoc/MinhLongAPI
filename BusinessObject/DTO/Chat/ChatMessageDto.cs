using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Chat
{
    public class ChatMessageDto
    {
        public Guid ChatMessageId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string MessageText { get; set; }
        public string? FileUrl { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
