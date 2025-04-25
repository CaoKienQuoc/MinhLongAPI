using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ReturnOrderDetail
    {
        [Key]
        public long ReturnOrderDetailId { get; set; }

        [Required]
        public long ReturnOrderId { get; set; }
        [ForeignKey(nameof(ReturnOrderId))]
        public ReturnOrder ReturnOrder { get; set; }

        [Required]
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        [Required]
        public int Quantity { get; set; }
    }
}
