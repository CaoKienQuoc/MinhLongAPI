using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class ProductCategoryResponseDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public string CreatedByName { get; set; }
        public string UpdatedByName { get; set; }

    }
}
