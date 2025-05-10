using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ChatMessage
    {
        [Key]
        public Guid ChatMessageId { get; set; } = Guid.NewGuid();

        // 1. Nếu bạn muốn hỗ trợ cả chat nhóm, thêm vào ChatRoomId
        //    ChatRoomId sẽ là bắt buộc nếu tin nhắn là nhóm, hoặc null khi tin nhắn 1‑1
        public Guid? ChatRoomId { get; set; }
        [ForeignKey(nameof(ChatRoomId))]
        public virtual ChatRoom ChatRoom { get; set; }

        // 2. Thông tin người gửi (bắt buộc)
        [Required]
        public Guid SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public virtual User Sender { get; set; }

        // 3. Thông tin người nhận (chỉ dùng khi chat 1‑1)
        public Guid? ReceiverId { get; set; }
        [ForeignKey(nameof(ReceiverId))]
        public virtual User Receiver { get; set; }

        // 4. Nội dung & file
        [MaxLength(2000)]
        public string MessageText { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        // 5. Thời gian và trạng thái đọc
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        // 6. Tính năng tự động xoá sau 7 ngày
        [NotMapped]
        public DateTime ExpiryDate => Timestamp.AddDays(7);
    }

}
