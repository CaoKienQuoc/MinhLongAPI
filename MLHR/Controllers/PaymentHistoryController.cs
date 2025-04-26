using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly IPaymentHistoryService _service;

        public PaymentHistoryController(IPaymentHistoryService service)
        {
            _service = service;
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllPaymentHistoriesAsync();
            return Ok(data);
        }

        [HttpGet("Payment-History-id/{PaymentHistoryId}")]
        public async Task<IActionResult> GetById(Guid PaymentHistoryId)
        {
            var data = await _service.GetPaymentHistoryByIdAsync(PaymentHistoryId);
            if (data == null) return NotFound("PaymentHistory not found.");
            return Ok(data);
        }

        [HttpGet("my-payment-history")]
        public async Task<IActionResult> GetPaymentHistoryForLoggedInUser()
        {
            var userId = GetLoggedInUserId(); // 🟢 Hàm này nên lấy từ JWT claims
            if (userId == null)
            {
                return Unauthorized(new { message = "User chưa đăng nhập." });
            }

            var histories = await _service.GetPaymentHistoriesByUserIdAsync(userId.Value);
           /* if (histories == null || !histories.Any())
            {
                return NotFound(new { message = "Không có lịch sử thanh toán nào." });
            }*/

            return Ok(histories);
        }

        private Guid? GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return null;
            return Guid.TryParse(userIdClaim.Value, out var id) ? id : null;
        }

        [HttpPost("test-reminder")]
        public async Task<IActionResult> TestSendReminder()
        {
            await _service.SendDebtRemindersAsync();
            return Ok("Gửi nhắc hạn xong!");
        }

        [HttpGet("export-invoice/{paymentHistoryId}")]
        public async Task<IActionResult> ExportInvoicePdf(Guid paymentHistoryId)
        {
            // ✅ Bước 1: Lấy thông tin PaymentHistoryDto
            var paymentHistory = await _service.GetPaymentHistoryByIdAsync(paymentHistoryId);
            if (paymentHistory == null)
                return NotFound("Không tìm thấy thông tin thanh toán.");

            // ✅ Bước 2: Sinh file PDF
            var pdfBytes = await _service.GenerateInvoicePdfAsync(paymentHistory);

            // ✅ Bước 3: Trả file PDF cho frontend
            return File(pdfBytes, "application/pdf", $"HoaDon-{paymentHistory.OrderCode}.pdf");
        }


        [HttpGet("dashboard/total-paid")]
        [Authorize]
        public async Task<IActionResult> GetTotalPaid()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == null) return Unauthorized();

            var total = await _service.GetTotalPaidAsync(userId);
            return Ok(new { TotalPaid = total });
        }

        [HttpGet("dashboard/today-paid")]
        [Authorize]
        public async Task<IActionResult> GetTodayPaid()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == null) return Unauthorized();

            var total = await _service.GetTodayPaidAsync(userId);
            return Ok(new { Date = DateTime.Today, TotalPaid = total });
        }

        [HttpGet("dashboard/month-paid")]
        [Authorize]
        public async Task<IActionResult> GetMonthPaid()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == null) return Unauthorized();

            var total = await _service.GetMonthPaidAsync(userId);
            return Ok(new { Month = DateTime.Now.Month, Year = DateTime.Now.Year, TotalPaid = total });
        }

        [HttpGet("dashboard/total-debt")]
        [Authorize]
        public async Task<IActionResult> GetRemainingDebt()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == null) return Unauthorized();

            var total = await _service.GetRemainingDebtAsync(userId);
            return Ok(new { RemainingDebt = total });
        }

    }

}
