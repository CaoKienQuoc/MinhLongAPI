using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Repo.IRepository;
using Repo.Repository;
using Services.Exceptions;
using Services.IService;
using Services.Service;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;

        public PaymentController(IPaymentService paymentService, IOrderService orderService)
        {
            _paymentService = paymentService;
            _orderService = orderService;
        }


        [HttpPost]
        [Route("{userId:Guid}")]
        public async Task<IActionResult> SendPaymentLink(Guid userId, CreatePaymentRequest request)
        {
            try
            {
                var result = await _paymentService.SendPaymentLink(userId, request);
                if (result == null)
                {
                    return BadRequest(new { message = "Không thể tạo liên kết thanh toán." });
                }
                return Ok(result);
            }
            catch (BusinessException ex)
            {
                // Trả về mã lỗi do bạn chỉ định, ví dụ 404 hoặc 400
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log lại lỗi thật nếu cần
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        [Route("debt-pay/{userId:Guid}")]
        public async Task<IActionResult> SendPaymentLinkDebtPay(Guid userId, CreatePaymentRequest request)
        {
            try
            {
                var result = await _paymentService.SendPaymentLinkDebtPay(userId, request);
                if (result == null)
                {
                    return BadRequest(new { message = "Không thể tạo liên kết thanh toán." });
                }
                return Ok(result);
            }
            catch (BusinessException ex)
            {
                // Trả về mã lỗi do bạn chỉ định, ví dụ 404 hoặc 400
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log lại lỗi thật nếu cần
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }


        [HttpGet("paymentconfirm")]
        public async Task<IActionResult> PaymentConfirm()
        {
            if (!Request.Query.ContainsKey("orderCode"))
                return Redirect("https://minhlong.mlhr.org/api/Payment/payment-fail");

            try
            {
                // 🔹 B1. Lấy tham số từ query
                long orderCode = long.Parse(Request.Query["orderCode"]!);
                decimal amount = decimal.Parse(Request.Query["amount"]!);
                string accountId = Request.Query["accountId"]!;
                Guid orderId = Guid.Parse(Request.Query["orderId"]!);

                // 🔹 B3. Tìm đơn hàng liên kết với orderCode
                var order = await _orderService.GetOrderByIdAsync(orderId); // nếu bạn lưu orderCode trong bảng Order
                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng tương ứng với orderCode.");

                var paymentHistory = await _paymentService.GetPaymentHistoryByOrderIdAsync(orderId);
                
                DateTime existingTransactionDate = DateTime.Now;
            
                // 🔹 B4. Chuẩn bị dữ liệu xác nhận
                var queryRequest = new QueryRequest
                {
                    userId = accountId,
                    price = amount,
                    Paymentlink = orderCode.ToString(),
                    orderCode = (int)orderCode,
                    OrderId = order.OrderId,
                    Url = Request.QueryString.Value!
                };

                var result = await _paymentService.ConfirmPayment(Request.QueryString.Value!, queryRequest);
                string formattedAmount2 = $"{amount:N0} VND";

                /*if (result != null && result.code == "00")
                {

                    string html = $@"
                    <html>
                    <head>
                    <meta charset='UTF-8'>
                    <title>Thanh toán thành công</title>
                    </head>
                    <body style='text-align:center;font-family:sans-serif; padding: 40px'>
                    <h1 style='color:green'>✅ BẠN ĐÃ THANH TOÁN THÀNH CÔNG</h1>
                    <p><strong>Mã đơn hàng (orderCode):</strong> {order.OrderCode}</p>
                    <p><strong>Số tiền (amount):</strong> {formattedAmount2}</p>
                    <p><strong>Ngày thanh toán (createDate):</strong> {existingTransactionDate:dd/MM/yyyy HH:mm:ss}</p>
                    <hr />
                    <p style='color:gray;'>Cảm ơn bạn đã sử dụng dịch vụ!</p>
    
                    <button onclick='window.location.href=""https://clone-ui-user.vercel.app/agency/payment""' 
                    style='margin-top:20px;padding:10px 20px;font-size:16px;border:none;
                    background-color:#007BFF;color:white;border-radius:5px;cursor:pointer;'>
                    Quay về trang chủ
                    </button>
                    </body>
                    </html>";

                    return Content(html, "text/html");

                }*/

                if (result != null && result.code == "00")
                {
                    return Redirect("https://www.minhlongshop.com.vn/agency/payment");
                }


                return Redirect("https://minhlong.mlhr.org/api/Payment/payment-fail");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi xác nhận thanh toán: " + ex.Message);
                return Redirect("https://minhlong.mlhr.org/api/Payment/payment-fail");
            }
        }
        

        // ✅ Trang báo thất bại
        [HttpGet("payment-fail")]
        public IActionResult PaymentFail()
        {
            

            return Content($@"
                <html>
                <head>
                <meta charset='UTF-8'>
                <title>Thất bại</title>
                </head>
                <body style='text-align:center;font-family:sans-serif'>
                <h1 style='color:red'>BẠN ĐÃ THANH TOÁN THẤT BẠI</h1>
                    <p>Giao dịch không thành công hoặc dữ liệu phản hồi không hợp lệ.</p>
                    <p>Xin vui lòng thử lại hoặc liên hệ hỗ trợ.</p>
                    <button onclick='window.location.href=""https://www.minhlongshop.com.vn/agency/payment""' 
                    style='margin-top:20px;padding:10px 20px;font-size:16px;border:none;background-color:#007BFF;color:white;border-radius:5px;cursor:pointer;'>
                    Quay về trang chủ
                    </button>
                    </body>
                    </html>", "text/html");

        }

    }
}
