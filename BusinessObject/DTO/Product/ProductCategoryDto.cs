using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTO.Product
{
    public class ProductCategoryDto
    {
        public string CategoryName { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
