using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnOrderImage
    {
        [Key]
        public long ReturnOrderImageId { get; set; }

        [Required]
        public long ReturnOrderId { get; set; }
        [ForeignKey(nameof(ReturnOrderId))]
        public ReturnOrder ReturnOrder { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; }
    }
}
