using BusinessObject.DTO.PaymentDTO;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Repo.IRepository;
using Repo.Repository;
using Services.Exceptions;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Services.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly PayOS _payOS;
        private readonly PayOSSettings _payOSSettings;
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IOrderService _orderService;
        private readonly HttpClient _client;
        private readonly IPaymentHistoryRepository _repository;
        private readonly IAgencyScoreHistoryRepository _agencyScoreRepository;
        private readonly IAgencyLevelRepository _agencyLevelRepository;
        private readonly IAgencyPromotionRequestRepository _agencyPromotionRepository;
        private readonly IAgencyAccountRepository _agencyRepository;

        // Constructor có đầy đủ các dependency
        public PaymentService(IOptions<PayOSSettings> payOSSettings,
                              IPaymentRepository paymentRepository,
                              IOrderRepository orderRepository,
                              IConfiguration configuration,
                              IUserRepository userRepository,
                              HttpClient client,
                              IOrderService orderService,
                              IPaymentHistoryRepository repository,
                              IAgencyScoreHistoryRepository agencyScoreHistory,
                              IAgencyLevelRepository agencyLevel,
                              IAgencyPromotionRequestRepository agencyPromotionRequest,
                              IAgencyAccountRepository agencyAccount)
        {
            // Kiểm tra nếu payOSSettings bị null
            _payOSSettings = payOSSettings?.Value ?? throw new ArgumentNullException(nameof(payOSSettings));

            // Kiểm tra nếu các giá trị trong _payOSSettings là null
            if (string.IsNullOrEmpty(_payOSSettings.ClientId) ||
                string.IsNullOrEmpty(_payOSSettings.ApiKey) ||
                string.IsNullOrEmpty(_payOSSettings.ChecksumKey))
            {
                throw new Exception("Cấu hình PayOS không hợp lệ. Vui lòng kiểm tra appsettings.json");
            }

            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _client = client;
            _repository = repository;
            _agencyScoreRepository = agencyScoreHistory ?? throw new ArgumentNullException(nameof(agencyScoreHistory));
            _agencyLevelRepository = agencyLevel ?? throw new ArgumentNullException(nameof(agencyLevel));
            _agencyPromotionRepository = agencyPromotionRequest ?? throw new ArgumentNullException(nameof(agencyPromotionRequest));
            _agencyRepository = agencyAccount ?? throw new ArgumentNullException(nameof(agencyAccount));
        }
    
        public async Task<CreatePaymentResult> SendPaymentLink(Guid accountId, CreatePaymentRequest request)
        {
            try
            {

                int amountToPay = (int)request.Price;
                await ValidateDebtBeforePaymentAsync(accountId, request.OrderId ?? Guid.Empty, amountToPay);



                var order = await _orderRepository.SingleOrDefaultAsync(p => p.OrderId == request.OrderId);
                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");

                // ✅ Dùng OrderCode đã có
                long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var agency = await _userRepository.GetAgencyAccountByUserIdAsync(accountId);
                if (agency == null) throw new Exception("Tài khoản không hợp lệ.");

                int amount = (int)request.Price;
                string description = request.Description;
                string? clientId = _configuration["PayOS:ClientId"];
                var apikey = _configuration["PayOS:APIKey"];
                var checksumkey = _configuration["PayOS:ChecksumKey"];
                var returnurlfail = _configuration["PayOS:ReturnUrlFail"];

                // ✅ returnUrl chỉ cần OrderId
                //string returnUrl = $"http://localhost:5214/api/Payment/paymentconfirm" +
                string returnUrl = $"https://minhlong.mlhr.org/api/Payment/paymentconfirm" +
                   $"?orderCode={orderCode}" +
                   $"&accountId={accountId}" +
                   $"&amount={request.Price}"+
                   $"&orderId={request.OrderId}";


                var signatureData = new Dictionary<string, object>
                {
                    { "amount", amount },
                    { "cancelUrl", returnurlfail },
                    { "description", description },
                    { "expiredAt", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    { "orderCode", orderCode },
                    { "returnUrl", returnUrl }
                    };

                var sortedSignatureData = new SortedDictionary<string, object>(signatureData);
                var dataForSignature = string.Join("&", sortedSignatureData.Select(p => $"{p.Key}={p.Value}"));
                var signature = ComputeHmacSha256(dataForSignature, checksumkey);

                PayOS pos = new PayOS(clientId, apikey, checksumkey);

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: amount,
                    description: description,
                    items: new List<ItemData> { new ItemData(agency.AgencyName, 1, amount) },
                    cancelUrl: returnurlfail,
                    returnUrl: returnUrl,
                    signature: signature,
                    buyerName: agency.AgencyName,
                    expiredAt: (int)DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds()
                );

                var createPaymentResult = await pos.createPaymentLink(paymentData);
                return createPaymentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tạo liên kết thanh toán: {ex.Message}");
                throw;
            }
        }

        private async Task ValidateDebtBeforePaymentAsync(Guid userId, Guid orderId, int amountToPay)
        {
            var creditLimit = await _repository.GetCreditLimitByUserIdAsync(userId);
            if (!creditLimit.HasValue) return;

            var totalDebt = await _repository.GetTotalRemainingDebtAmountByUserIdAsync(userId);
            if (totalDebt >= creditLimit.Value)
            {
                throw new BusinessException("Tổng công nợ hiện tại đã vượt quá hạn mức cho phép. Vui lòng thanh toán trước khi tiếp tục.", 404);
            }

            // 🧾 Lấy số tiền cần thanh toán của đơn hàng (không phải tổng nợ toàn bộ)
            var order = await _orderRepository.SingleOrDefaultAsync(p => p.OrderId == orderId);
            if (order == null)
                throw new BusinessException("Không tìm thấy đơn hàng.", 404);

            var totalOrderAmount = (int)order.FinalPrice; // hoặc order.TotalDebt nếu có cột riêng

            int minimumAcceptable = (int)Math.Ceiling(totalOrderAmount * 0.10); // Làm tròn lên nếu cần
            if (amountToPay <= 0 || amountToPay < minimumAcceptable)
            {
                throw new BusinessException(
                    $"Số tiền thanh toán không hợp lệ. Bạn cần thanh toán tối thiểu {minimumAcceptable:N0}đ (10% giá trị đơn hàng).",
                    400
                );
            }


            var remainingDebt = totalOrderAmount - amountToPay;

            if (remainingDebt >= creditLimit.Value)
            {
                var minRequiredPayment = totalOrderAmount - creditLimit.Value + 1;
                throw new BusinessException(
                    $"Bạn cần thanh toán tối thiểu {minRequiredPayment:N0}đ để công nợ đơn hàng không vượt quá hạn mức {creditLimit.Value:N0}đ.",
                    404
                );
            }
        }


        public async Task<CreatePaymentResult> SendPaymentLinkDebtPay(Guid accountId, CreatePaymentRequest request)
        {
            try
            {

                var order = await _orderRepository.SingleOrDefaultAsync(p => p.OrderId == request.OrderId);
                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");

                // ✅ Dùng OrderCode đã có
                long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var agency = await _userRepository.GetAgencyAccountByUserIdAsync(accountId);
                if (agency == null) throw new Exception("Tài khoản không hợp lệ.");

                int amount = (int)request.Price;
                string description = request.Description;
                string? clientId = _configuration["PayOS:ClientId"];
                var apikey = _configuration["PayOS:APIKey"];
                var checksumkey = _configuration["PayOS:ChecksumKey"];
                var returnurlfail = _configuration["PayOS:ReturnUrlFail"];

                // ✅ returnUrl chỉ cần OrderId
                //string returnUrl = $"http://localhost:5214/api/Payment/paymentconfirm" +
                string returnUrl = $"https://minhlong.mlhr.org/api/Payment/paymentconfirm" +
                   $"?orderCode={orderCode}" +
                   $"&accountId={accountId}" +
                   $"&amount={request.Price}" +
                   $"&orderId={request.OrderId}";


                var signatureData = new Dictionary<string, object>
                {
                    { "amount", amount },
                    { "cancelUrl", returnurlfail },
                    { "description", description },
                    { "expiredAt", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    { "orderCode", orderCode },
                    { "returnUrl", returnUrl }
                    };

                var sortedSignatureData = new SortedDictionary<string, object>(signatureData);
                var dataForSignature = string.Join("&", sortedSignatureData.Select(p => $"{p.Key}={p.Value}"));
                var signature = ComputeHmacSha256(dataForSignature, checksumkey);

                PayOS pos = new PayOS(clientId, apikey, checksumkey);

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: amount,
                    description: description,
                    items: new List<ItemData> { new ItemData(agency.AgencyName, 1, amount) },
                    cancelUrl: returnurlfail,
                    returnUrl: returnUrl,
                    signature: signature,
                    buyerName: agency.AgencyName,
                    expiredAt: (int)DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds()
                );

                var createPaymentResult = await pos.createPaymentLink(paymentData);
                return createPaymentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tạo liên kết thanh toán: {ex.Message}");
                throw;
            }
        }





        private string ComputeHmacSha256(string data, string checksumKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public async Task<StatusPayment> ConfirmPayment(string queryString, QueryRequest requestquery)
        {

            // ✅ Sử dụng TimeZoneInfo để đảm bảo chính xác
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);


            try
            {
                

                var getUrl = $"https://api-merchant.payos.vn/v2/payment-requests/{requestquery.Paymentlink}";


                Guid? userId = Guid.TryParse(requestquery.userId, out var accountGuid) ? accountGuid : (Guid?)null;
                var agency = await _userRepository.GetAgencyAccountByUserIdAsync(userId);

                // Gửi request đến PayOS
                var request = new HttpRequestMessage(HttpMethod.Get, getUrl);
                request.Headers.Add("x-client-id", _configuration["PayOS:ClientId"]);
                request.Headers.Add("x-api-key", _configuration["PayOS:APIKey"]);

                var response = await _client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Không gửi được yêu cầu tới PayOS.");

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JObject.Parse(responseContent);
                var status = responseObject["data"]?["status"]?.ToString();

                /*if (status == "PAID")
                    return null!;*/

                decimal paidAmount = requestquery.price;

                // B1. Lấy thông tin đơn hàng
                var order = await _orderRepository.SingleOrDefaultAsync(p => p.OrderId == requestquery.OrderId);
                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng.");

                decimal totalOrderAmount = order.FinalPrice;
                decimal newRemainingDebt = 0;

                // B2. Kiểm tra đã có PaymentHistory cho đơn này chưa
                var existingHistory = await _paymentRepository.GetPaymentHistoryByOrderIdAsync(order.OrderId);

                if (existingHistory != null)
                {
                    // Cộng dồn tiền thanh toán
                    existingHistory.PaymentAmount += paidAmount;

                    if (existingHistory.PaymentAmount == totalOrderAmount)
                    {
                        existingHistory.RemainingDebtAmount = 0;
                        existingHistory.Status = "PAID";
                    }
                    else
                    {
                        existingHistory.RemainingDebtAmount = totalOrderAmount - existingHistory.PaymentAmount;
                        existingHistory.Status = "PARTIALLY_PAID";
                    }

                    existingHistory.UpdatedAt = vnNow;
                    await _paymentRepository.UpdatePaymentHistoryAsync(existingHistory);
                }
                else
                {
                    // Giao dịch đầu tiên
                    var statusFlag = paidAmount >= totalOrderAmount ? "PAID" : "PARTIALLY_PAID";
                    newRemainingDebt = paidAmount >= totalOrderAmount ? 0 : totalOrderAmount - paidAmount;

                    // 1) Lấy AgencyAccountLevel (có LevelId)
                    var accountLevel = await _paymentRepository
                        .GetAgencyAccountLevelByAgencyIdAsync(agency.AgencyId);
                    if (accountLevel == null)
                        throw new Exception("AgencyAccountLevel not found.");

                    long levelId = accountLevel.LevelId;

                    // 2) Lấy số ngày PaymentTerm từ AgencyLevelRepository
                    var paymentTermDays = await _agencyLevelRepository
                        .GetPaymentTermByLevelIdAsync(levelId)
                        ?? throw new Exception($"Không tìm thấy PaymentTerm cho level {levelId}");

                    // 3) Tính DueDate = ngày thanh toán đầu + paymentTermDays
                    DateTime firstPaymentDate = DateTime.Now;
                    DateTime computedDueDate = firstPaymentDate.AddDays(paymentTermDays);

                    existingHistory = new PaymentHistory
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = "PayOS",
                        PaymentDate = vnNow,
                        Status = statusFlag,
                        TotalAmountPayment = totalOrderAmount,
                        RemainingDebtAmount = newRemainingDebt, 
                        PaymentAmount = paidAmount,
                        CreatedAt = vnNow,
                        UpdatedAt = vnNow,  
                        SerieNumber = $"SER-{vnNow.Ticks}",
                        UserId = userId.Value,
                        DueDate = computedDueDate
                    };

                    // ❗ KHÔNG gán PaymentHistoryId ở đây

                    await _paymentRepository.InsertPaymentHistoryAsync(existingHistory);
                    await _paymentRepository.SaveChangesAsync(); // 👈 Lúc này Id mới được sinh
                    await _orderService.ProcessPaymentAsync(order.OrderId);

                }


                if (newRemainingDebt > 0 && agency != null)
                {
                    var agencyLevel = await _paymentRepository.GetAgencyAccountLevelByAgencyIdAsync(agency.AgencyId);
                    if (agencyLevel == null)
                        throw new Exception("AgencyAccountLevel not found.");

                    agencyLevel.TotalDebtValue += newRemainingDebt;
                    agencyLevel.ChangeDate = DateTime.Now;

                    await _paymentRepository.UpdateAgencyAccountLevelAsync(agencyLevel);
                    await _paymentRepository.SaveChangesAsync();
                }

                // ✅ newHistory.PaymentHistoryId đã được sinh tự động, dùng được ở đây:
                var transaction = new PaymentTransaction
                {
                    PaymentHistoryId = existingHistory.PaymentHistoryId, // ✅ lấy từ EF sau khi lưu
                    PaymentDate = vnNow,
                    Amount = paidAmount,
                    PaymentStatus = "PAID",
                    TransactionReference = requestquery.Paymentlink
                };

                await _paymentRepository.InsertPaymentTransactionAsync(transaction);
                //order.Status = "Paid";
                await _paymentRepository.SaveChangesAsync();

                /*// ✅ Nếu thanh toán đủ & đúng hạn => Cộng điểm
                if (existingHistory.Status == "PAID")
                {
                    var reason = "Thanh toán đơn hàng đúng hạn";
                    var existingScore = await _agencyScoreRepository.GetByAgencyIdAndReasonAsync(agency.AgencyId, reason);

                    if (existingScore != null)
                    {
                        // ✅ Cập nhật điểm nếu đã có bản ghi
                        existingScore.ScoreChange += 5;
                        existingScore.CreatedDate = transaction.PaymentDate;
                        await _agencyScoreRepository.UpdateAsync(existingScore);
                    }
                    else
                    {
                        // ✅ Thêm mới nếu chưa có
                        var scoreEntry = new AgencyScoreHistory
                        {
                            AgencyId = agency.AgencyId,
                            ScoreChange = 5,
                            Reason = "Thanh toán đơn hàng đúng hạn",
                            CreatedDate = transaction.PaymentDate
                        };
                        await _agencyScoreRepository.AddScoreAsync(scoreEntry);
                    }

                    await _agencyScoreRepository.SaveChangesAsync();


                    var totalScore = await _agencyScoreRepository.GetTotalScoreByAgencyIdAsync(agency.AgencyId);
                    var currentLevel = await _agencyLevelRepository.GetCurrentLevelByAgencyIdAsync(agency.AgencyId);

                    if (currentLevel == 3 && totalScore >= 1000)
                    {
                        bool exists = await _agencyPromotionRepository.HasPendingRequestAsync(agency.AgencyId, 2);
                        if (!exists)
                        {
                            var promotionRequest = new AgencyPromotionRequest
                            {
                                AgencyId = agency.AgencyId,
                                CurrentLevelId = 3,
                                SuggestedLevelId = 2,
                                TotalScore = totalScore,
                                Status = "Pending",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _agencyPromotionRepository.AddAsync(promotionRequest);
                            await _agencyPromotionRepository.SaveChangesAsync();
                        }
                    }
                    else if (currentLevel == 2 && totalScore >= 5000)
                    {
                        bool exists = await _agencyPromotionRepository.HasPendingRequestAsync(agency.AgencyId, 1);
                        if (!exists)
                        {
                            var promotionRequest = new AgencyPromotionRequest
                            {
                                AgencyId = agency.AgencyId,
                                CurrentLevelId = 2,
                                SuggestedLevelId = 1,
                                TotalScore = totalScore,
                                Status = "Pending",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _agencyPromotionRepository.AddAsync(promotionRequest);
                            await _agencyPromotionRepository.SaveChangesAsync();
                        }
                    }
                }*/


                if (existingHistory.Status == "PAID")
                {
                    var currentLevel = await _agencyLevelRepository.GetCurrentLevelByAgencyIdAsync(agency.AgencyId);
                    var allLevels = await _agencyLevelRepository.GetAllLevelsAsync(); // Trả về List<AgencyLevel>

                    // Sắp xếp theo DiscountPercentage tăng dần
                    var orderedLevels = allLevels.OrderBy(l => l.DiscountPercentage).ToList();

                    // Tìm index cấp hiện tại
                    var currentIndex = orderedLevels.FindIndex(l => l.LevelId == currentLevel);

                    // Không cộng điểm nếu là cấp cao nhất
                    if (currentIndex >= 0 && currentIndex < orderedLevels.Count - 1)
                    {
                        var nextLevel = orderedLevels[currentIndex + 1]; // Cấp cao hơn gần nhất
                        decimal ratio = 1.0m; // 1 triệu = 1 điểm

                        // ❗ Không ép kiểu int — cho phép điểm lẻ
                        decimal addedScore = (transaction.Amount / 1_000_000m) * ratio;
                        var paymentId = transaction.PaymentHistoryId;
                        var payment = await _repository.GetByIdAsync(paymentId);
                        var orderCode = await _orderRepository.GetOrderByIdAsync(payment.OrderId);

                        var reason = $"Thanh toán đơn hàng #{orderCode.OrderCode} đúng số tiền";

                        var existingScore = await _agencyScoreRepository.GetByAgencyIdAndReasonAsync(agency.AgencyId, reason);
                        if (existingScore == null)
                        {
                            var scoreEntry = new AgencyScoreHistory
                            {
                                AgencyId = agency.AgencyId,
                                ScoreChange = (long)addedScore,
                                Reason = reason,
                                CreatedDate = transaction.PaymentDate
                            };
                            await _agencyScoreRepository.AddScoreAsync(scoreEntry);
                            await _agencyScoreRepository.SaveChangesAsync();

                            agency.AgencyScore = scoreEntry.ScoreChange;
                            await _agencyRepository.UpdateAsync(agency);

                            Console.WriteLine($"✅ +{addedScore} điểm cho đại lý {agency.AgencyName} - {reason}");
                        }

                        var totalScore = await _agencyScoreRepository.GetTotalScoreByAgencyIdAsync(agency.AgencyId);

                        // Nếu đủ 2000 điểm và chưa gửi request thăng cấp
                        if (totalScore >= 2000)
                        {
                            bool exists = await _agencyPromotionRepository.HasPendingRequestAsync(agency.AgencyId, nextLevel.LevelId);
                            if (!exists)
                            {
                                var promotionRequest = new AgencyPromotionRequest
                                {
                                    AgencyId = agency.AgencyId,
                                    //CurrentLevelId = currentLevel,
                                    SuggestedLevelId = nextLevel.LevelId,
                                    TotalScore = totalScore,
                                    Status = "Pending",
                                    CreatedAt = vnNow
                                };
                                await _agencyPromotionRepository.AddAsync(promotionRequest);
                                await _agencyPromotionRepository.SaveChangesAsync();
                            }
                        }
                    }
                }




                return new StatusPayment
                {
                    code = "00",
                    Data = new data
                    {
                        status = "PAID",
                        amount = paidAmount
                    }
                };

            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi xác nhận thanh toán: " + ex.Message);
            }
        }

        public async Task<PaymentTransaction?> GetTransactionByReferenceAsync(Guid paymentHistoryId)
        {
            return await _paymentRepository.GetTransactionByReferenceAsync(paymentHistoryId);
        }

        public async Task<PaymentHistory?> GetPaymentHistoryByOrderIdAsync(Guid orderId)
        {
            return await _paymentRepository.GetPaymentHistoryByOrderIdAsync(orderId);
        }
    }
}
