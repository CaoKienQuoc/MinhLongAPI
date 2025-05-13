using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ChatMessageImage
    {
        [Key]
        public Guid ImageId { get; set; }
        public Guid ChatMessageId { get; set; }
        public string ImageUrl { get; set; }
        public DateTime UploadedAt { get; set; }

        public string PublicId { get; set; }

        // Navigation property (nếu bạn dùng Entity Framework)
        public ChatMessage ChatMessage { get; set; }
    }
}
