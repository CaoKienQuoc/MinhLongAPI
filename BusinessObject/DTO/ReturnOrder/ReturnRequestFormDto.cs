using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace BusinessObject.DTO.ReturnOrder
{
    public class ReturnRequestFormDto
    {
        [Required]
        public Guid OrderId { get; set; }

        public List<IFormFile>? Images { get; set; }

        // Truyền từ client dạng JSON: 
        // [{"orderDetailId":"...", "quantity":2, "reason":"Lý do..."}]
        [Required]
        public string ItemsJson { get; set; } = string.Empty;

        [NotMapped]
        public List<FlattenedReturnItemDto> Items =>
            string.IsNullOrWhiteSpace(ItemsJson)
                ? new List<FlattenedReturnItemDto>()
                : JsonConvert.DeserializeObject<List<FlattenedReturnItemDto>>(ItemsJson) ?? new List<FlattenedReturnItemDto>();
    }



}
