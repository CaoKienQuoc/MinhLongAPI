using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Contract
    {
        [Key]
        public long ContractId { get; set; }

        public long? AgencyId { get; set; }
        [ForeignKey("AgencyId")]
        public AgencyAccount AgencyAccount { get; set; }

        public long? EmployeeId { get; set; }  // optional
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public string? FileName { get; set; }    // nullable

        public string? FilePath { get; set; }    // nullable

        public string? FileType { get; set; }    // nullable

        public DateTime CreatedAt { get; set; } = DateTime.Now; // default vẫn là now
    }
}
