using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;
using QuestPDF.Helpers;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Services.Service
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly IPaymentHistoryRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _mailService;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public PaymentHistoryService(IPaymentHistoryRepository repository, ICacheService cacheService, IEmailService mailService, IUserRepository userRepository, IOrderRepository orderRepository)
        {
            _repository = repository;
            _cacheService = cacheService;
            _mailService = mailService;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        public async Task<PaymentHistoryDto> GetPaymentHistoryByIdAsync(Guid id)
        {
            var payment = await _repository.GetByIdAsync(id);

            if (payment == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy lịch sử thanh toán với ID = {id}");
            }

            var dueDate = payment.CreatedAt.AddMonths(3);
            return new PaymentHistoryDto
            {
                PaymentHistoryId = payment.PaymentHistoryId,
                OrderId = payment.OrderId,
                OrderCode = payment.Order?.OrderCode ?? "N/A",

                AgencyId = payment.Order?.RequestProduct?.AgencyId ?? 0,
                AgencyName = payment.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",

                PaymentMethod = payment.PaymentMethod,
                PaymentDate = payment.PaymentDate,
                SerieNumber = payment.SerieNumber,
                Status = payment.Status,
                TotalAmountPayment = payment.TotalAmountPayment,
                RemainingDebtAmount = payment.RemainingDebtAmount,
                PaymentAmount = payment.PaymentAmount,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                TransactionReference = payment.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A", // 👈 Lấy TransactionReference
                DueDate = payment.DueDate,
                DebtStatus = GetDebtStatus(payment.DueDate, payment.Status)
            };
        }


        public async Task<List<PaymentHistoryDto>> GetAllPaymentHistoriesAsync()
        {
            var histories = await _repository.GetAllAsync();

            return histories.OrderByDescending(re => re.CreatedAt).Select(ph =>
            {
                var dueDate = ph.CreatedAt.AddMonths(3);
                return new PaymentHistoryDto
                {
                    PaymentHistoryId = ph.PaymentHistoryId,
                    OrderId = ph.OrderId,
                    OrderCode = ph.Order?.OrderCode ?? "N/A",
                    AgencyId = ph.Order?.RequestProduct?.AgencyId ?? 0,
                    AgencyName = ph.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                    PaymentMethod = ph.PaymentMethod,
                    PaymentDate = ph.PaymentDate,
                    SerieNumber = ph.SerieNumber,
                    Status = ph.Status,
                    TotalAmountPayment = ph.TotalAmountPayment,
                    RemainingDebtAmount = ph.RemainingDebtAmount,
                    PaymentAmount = ph.PaymentAmount,
                    CreatedAt = ph.CreatedAt,
                    UpdatedAt = ph.UpdatedAt,
                    TransactionReference = ph.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A",
                    DueDate = ph.DueDate,
                    DebtStatus = GetDebtStatus(ph.DueDate, ph.Status),
                    UserId = ph.UserId,
                };
            }).ToList();
        }

        public async Task<List<PaymentHistoryDto>> GetPaymentHistoriesByUserIdAsync(Guid userId)
        {
            var payments = await _repository.GetPaymentHistoryByUserIdAsync(userId);

            return payments.Select(ph =>
            {
                var dueDate = ph.CreatedAt.AddMonths(3);
                return new PaymentHistoryDto
                {
                    PaymentHistoryId = ph.PaymentHistoryId,
                    OrderId = ph.OrderId,
                    OrderCode = ph.Order?.OrderCode ?? "N/A",
                    AgencyId = ph.Order?.RequestProduct?.AgencyId ?? 0,
                    AgencyName = ph.Order?.RequestProduct?.AgencyAccount?.AgencyName ?? "Unknown",
                    PaymentMethod = ph.PaymentMethod,
                    PaymentDate = ph.PaymentDate,
                    SerieNumber = ph.SerieNumber,
                    Status = ph.Status,
                    TotalAmountPayment = ph.TotalAmountPayment,
                    RemainingDebtAmount = ph.RemainingDebtAmount,
                    PaymentAmount = ph.PaymentAmount,
                    CreatedAt = ph.CreatedAt,
                    UpdatedAt = ph.UpdatedAt,
                    TransactionReference = ph.PaymentTransactions?.FirstOrDefault()?.TransactionReference ?? "N/A",
                    DueDate = ph.DueDate,
                    DebtStatus = GetDebtStatus(ph.DueDate, ph.Status)
                };
            }).ToList();
        }

        /*private string GetDebtStatus(DateTime dueDate)
        {
            var daysLeft = (dueDate - DateTime.UtcNow).TotalDays;

            if (daysLeft > 10)
                return "StillValid";
            else if (daysLeft <= 10 && daysLeft >= 0)
                return "NearDue";
            else
                return "OverDue";
        }*/

        private string GetDebtStatus(DateTime dueDate, string paymentStatus)
        {
            // 1) Nếu đã thanh toán xong thì luôn DebtFree
            if (string.Equals(paymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
                return "DebtFree";

            // 2) Lấy giờ Việt Nam hiện tại (giữ luôn phần giờ)
            var vietnamNow = DateTime.UtcNow.AddHours(7);

            // 3) Xác định thời điểm hết hạn: 0:01 AM ngày tiếp theo sau dueDate
            DateTime expirationThreshold = dueDate.Date               // chuyển dueDate về 00:00 cùng ngày
                                      .AddDays(1)                     // cộng thêm 1 ngày → 00:00 ngày hôm sau
                                      .AddMinutes(1);                 // cộng thêm 1 phút → 00:01

            // 4) Nếu đã vượt qua ngưỡng 0:01 ngày hôm sau thì gọi là OverDue
            if (vietnamNow >= expirationThreshold)
                return "OverDue";

            // 5) Còn lại, tính số ngày còn lại (chỉ so sánh về phần Date để đánh giá StillValid / NearDue)
            var daysLeft = (dueDate.Date - vietnamNow.Date).TotalDays;

            if (daysLeft > 10)
                return "StillValid";
            else // 0 <= daysLeft <= 10
                return "NearDue";
        }



        public async Task SendDebtRemindersAsync()
        {
            var payments = await _repository.GetAllPaymentHistoryAsync(); // Đã include User (Email)

            foreach (var payment in payments)
            {
                var dueDate = payment.PaymentDate.AddMonths(3);
                //var dueDate = new DateTime(2025, 4, 15);
                var daysLeft = (dueDate - DateTime.UtcNow.Date).TotalDays;

                if (daysLeft <= 10 && daysLeft >= 0)
                {
                    string cacheKey = $"DebtReminder:{payment.OrderId}:{DateTime.UtcNow:yyyy-MM-dd}";
                    if (!await _cacheService.ExistsAsync(cacheKey))
                    {
                        // 🔥 Lấy Email từ bảng User
                        var email = payment.User?.Email;
                        if (string.IsNullOrEmpty(email) || payment.UserId == Guid.Empty)
                            continue;

                        // 🔥 Truy vấn AgencyAccount để lấy AgencyName theo UserId
                        var agencyAccount = await _userRepository.GetAgencyAccountByUserIdAsync(payment.UserId);
                        var agencyName = agencyAccount?.AgencyName;

                        var order = await _orderRepository.GetOrderByIdAsync(payment.OrderId);
                        var orderCode = order?.OrderCode;


                        if (!string.IsNullOrEmpty(agencyName))
                        {
                            await _mailService.SendEmailDebtReminderAsync(
                                email,
                                agencyName,
                                orderCode,
                                dueDate
                            );

                            await _cacheService.SetAsync(cacheKey, true, TimeSpan.FromDays(1));
                        }
                    }
                }
            }
        }

        public Task<decimal> GetTotalPaidAsync(Guid userId) =>
        _repository.GetTotalPaymentAmountByUserIdAsync(userId);

        public Task<decimal> GetTodayPaidAsync(Guid userId) =>
            _repository.GetPaymentAmountByDateAsync(userId, DateTime.Today);

        public Task<decimal> GetMonthPaidAsync(Guid userId)
        {
            var now = DateTime.Now;
            return _repository.GetPaymentAmountByMonthAsync(userId, now.Year, now.Month);
        }

        public Task<decimal> GetRemainingDebtAsync(Guid userId) =>
            _repository.GetRemainingDebtByUserIdAsync(userId);


        public async Task<byte[]> GenerateInvoicePdfAsync(PaymentHistoryDto paymentHistory)
        {
            var order = await _orderRepository.GetOrderByIdAsync(paymentHistory.OrderId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text("HÓA ĐƠN BÁN HÀNG")
                        .FontSize(24).SemiBold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Thông tin đơn hàng
                        column.Item().Text($"Tên đại lý: {paymentHistory.AgencyName}").FontSize(14);
                        column.Item().Text($"Mã đơn hàng: {order.OrderCode}").FontSize(14);
                        column.Item().Text($"Ngày đặt hàng: {order.OrderDate:dd/MM/yyyy}").FontSize(14);
                        column.Item().Text($"Ngày thanh toán: {paymentHistory.PaymentDate:dd/MM/yyyy}").FontSize(14);
                        column.Item().Text($"Mã giao dịch thanh toán: {paymentHistory.TransactionReference ?? "Không có"}").FontSize(14);

                        // Đường kẻ ngang
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Bảng sản phẩm
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Tên sản phẩm
                                columns.RelativeColumn(1); // Số lượng
                                columns.RelativeColumn(2); // Đơn giá
                                columns.RelativeColumn(2); // Thành tiền
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Tên sản phẩm").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá (VNĐ)").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền (VNĐ)").Bold();
                            });

                            // Body
                            foreach (var item in order.OrderDetails)
                            {
                                table.Cell().Element(CellStyle).Text(item.Product?.ProductName ?? "Sản phẩm");
                                table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("N0"));
                                table.Cell().Element(CellStyle).AlignRight().Text(item.TotalAmount.ToString("N0"));
                            }
                        });

                        // Đường kẻ ngang
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Tổng tiền
                        column.Item().AlignRight().Text($"Tổng tiền hàng: {order.OrderDetails.Sum(x => x.TotalAmount):N0} VNĐ")
                            .FontSize(14).SemiBold();

                        column.Item().AlignRight().Text($"Giảm giá: {order.Discount:N0} VNĐ")
                            .FontSize(14);

                        column.Item().AlignRight().Text($"Thành tiền sau giảm giá: {order.FinalPrice:N0} VNĐ")
                            .FontSize(16).Bold().FontColor(Colors.Red.Medium);

                        // Thanh toán
                        column.Item().PaddingTop(5).AlignRight().Text($"Số tiền đã thanh toán: {paymentHistory.PaymentAmount:N0} VNĐ")
                            .FontSize(14).FontColor(Colors.Green.Darken2);

                        column.Item().AlignRight().Text($"Số tiền còn nợ: {paymentHistory.RemainingDebtAmount:N0} VNĐ")
                            .FontSize(14).FontColor(Colors.Red.Medium);

                        // Ghi chú
                        column.Item().PaddingTop(20).Text("Cảm ơn quý khách đã mua hàng!")
                            .AlignCenter().FontSize(12).Italic();
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("© 2025 Công ty XYZ - Hotline: 0123 456 789")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .PaddingVertical(5)
                .PaddingHorizontal(2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }
    }
}
