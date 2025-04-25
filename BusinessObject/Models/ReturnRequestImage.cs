using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnRequestImage
    {
        [Key]
        public long ReturnRequestImageId { get; set; }

        [Required]
        public long ReturnRequestDetailId { get; set; }

        [ForeignKey(nameof(ReturnRequestDetailId))]
        public ReturnRequestDetail ReturnRequestDetail { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

}
