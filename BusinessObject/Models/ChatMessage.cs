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

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [MaxLength(2000)]
        public string MessageText { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        [NotMapped]
        public DateTime ExpiryDate => Timestamp.AddDays(7);

        // Navigation properties
        [ForeignKey(nameof(SenderId))]
        public virtual User Sender { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual User Receiver { get; set; }
    }
}
