using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class RegisterAccountContract
    {
        [Key]
        public long ContractId { get; set; }

        [Required]
        public int RegisterId { get; set; }

        [ForeignKey("RegisterId")]
        public RegisterAccount RegisterAccount { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? FileType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
