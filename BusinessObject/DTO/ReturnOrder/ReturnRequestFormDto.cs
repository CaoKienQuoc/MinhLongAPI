using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestFormDto
    {
        [Required]
        public Guid OrderId { get; set; }

        public List<IFormFile> Images { get; set; }

        [Required]
        public List<FlattenedReturnItemDto> Items { get; set; }
    }

}
