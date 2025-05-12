using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Batch
    {
        [Key]
        public long BatchId { get; set; }
        public long ImportTransactionDetailId { get; set; }
        public long ProductId { get; set; }
        public string BatchCode { get; set; }
        public decimal UnitCost { get; set; }
        public int Quantity { get; set; }
        public DateTime DateOfManufacture { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Unit{ get; set; }

        public bool SoldOut { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProfitMarginPercent { get; set; } // Phần trăm lợi nhuận
        public string Status { get; set; } // Available, Expired

        [ForeignKey("ImportTransactionDetailId")]
        public ImportTransactionDetail ImportTransactionDetail { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
