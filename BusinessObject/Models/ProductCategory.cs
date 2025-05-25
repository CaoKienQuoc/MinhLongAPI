using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ProductCategory
    {
        [Key]
        public long CategoryId { get; set; }

        [Required, MaxLength(255)]
        public string CategoryName { get; set; }


        public string Notes { get; set; }

        public bool IsActive { get; set; }

        public Guid CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User Creator { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid? UpdatedBy { get; set; }
        [ForeignKey("UpdatedBy")]
        public User Updater { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
