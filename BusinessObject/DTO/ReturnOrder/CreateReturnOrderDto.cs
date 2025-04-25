using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTO.ReturnOrder
{
    public class CreateReturnOrderDto
    {
        [Required]
        public long OrderId { get; set; }

        [Required]
        public long RequestExportId { get; set; }

        [Required, StringLength(1000)]
        public string Reason { get; set; }

        public List<ReturnOrderItemDto> Items { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}
