using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } // Tiêu đề thông báo

        [Required]
        public string Message { get; set; } // Nội dung

        public string? Url { get; set; } // Đường dẫn (nếu có) khi click vào

        public bool IsRead { get; set; } = false; // Đã đọc hay chưa

        public DateTime CreatedAt { get; set; }
    }
}
