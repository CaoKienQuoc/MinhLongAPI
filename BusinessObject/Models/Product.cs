using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Product
    {
        [Key]
        public long ProductId { get; set; }

        [Required, MaxLength(255)]
        public string ProductCode { get; set; }

        [Required, MaxLength(255)]
        public string ProductName { get; set; }

        public Guid CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User Creator { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid? UpdatedBy { get; set; }
        [ForeignKey("UpdatedBy")]
        public User Updater { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(50)]
        public string Unit { get; set; }

        public int? DefaultExpiration { get; set; }

        public long CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public ProductCategory Category { get; set; }

        public string Description { get; set; }

        public int? TaxId { get; set; }
        [ForeignKey("TaxId")]
        public TaxConfig TaxConfig { get; set; }

        // Mối quan hệ: Một Product có nhiều Image
        public List<Image> Images { get; set; } = new List<Image>();

        public long AvailableStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public ICollection<ExportTransactionDetail> ExportTransactionDetail { get; set; }
        public ICollection<ExportWarehouseReceipt> ExportWarehouseReceipt { get; set; }

        // Navigation Property: Một Product có thể có nhiều ProductDetail
        public ICollection<RequestProductDetail> RequestProductDetail { get; set; }
        public virtual ICollection<TemporaryStockExport> TemporaryStockExports { get; set; } = new List<TemporaryStockExport>();

    }
}
