    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace BusinessObject.DTO.PaymentDTO
    {
        public class PaymentHistoryDto
        {
            public Guid PaymentHistoryId { get; set; }
            public Guid OrderId { get; set; }

            public string OrderCode { get; set; }

            public long AgencyId { get; set; }              // 👈 ID đại lý
            public string AgencyName { get; set; }          // 👈 Tên đại lý

            public string PaymentMethod { get; set; }
            public DateTime PaymentDate { get; set; }
            public string SerieNumber { get; set; }
            public string Status { get; set; }

            public decimal TotalAmountPayment { get; set; }
            public decimal RemainingDebtAmount { get; set; }
            public decimal PaymentAmount { get; set; }

            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }

        public DateTime DueDate { get; set; } // 👈 Ngày hết hạn thanh toán
        public string DebtStatus { get; set; } // 👈 Trạng thái nợ

        public string TransactionReference { get; set; }
        // ---- Thêm vào
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } // Địa chỉ email của user
    }


}
