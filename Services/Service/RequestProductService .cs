using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using Services.IService;
using System.Linq;
using Repo.Repository;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Exceptions;
using MailKit.Search;
using Microsoft.AspNetCore.SignalR;
using SkiaSharp;
using System.Diagnostics;
using BusinessObject.DTO.RequestExport;
using BusinessObject.DTO.Product;

namespace Services.Service
{
    public class RequestProductService : IRequestProductService
    {
        private readonly IRequestProductRepository _requestProductRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        private readonly IBatchRepository _batchRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<NotificationHub> _hub;

        public RequestProductService(
            IRequestProductRepository requestProductRepository,
            IOrderRepository orderRepository,
            IBatchRepository batchRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IHubContext<NotificationHub> hub,
            IInventoryService inventoryService,
            IProductService productService)
        {
            _requestProductRepository = requestProductRepository;
            _orderRepository = orderRepository;
            _batchRepository = batchRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _hub = hub;
            _inventoryService = inventoryService;
            _productService = productService;
        }


        public async Task<List<RequestProductDto>> GetAllRequestsAsync()
        {
            var requests = await _requestProductRepository.GetAllRequestsAsync();

            return requests.Select(rp => new RequestProductDto
            {
                RequestProductId = rp.RequestProductId,
                RequestCode = rp.RequestCode,
                AgencyName = rp.AgencyAccount?.AgencyName ?? "Unknown",
                AgencyId = rp.AgencyId,
                //ApprovedBy = rp.ApprovedBy,
                RequestStatus = rp.RequestStatus,
                CreatedAt = rp.CreatedAt,
                RequestProductDetails = rp.RequestProductDetails.Select(d => new RequestProductDetailDto
                {
                    requestProductDetailId = d.RequestDetailId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.ProductName ?? "N/A",
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    UnitPrice = d.Price
                }).ToList()
            }).ToList();
        }

        public async Task<RequestProductDto> GetRequestByIdAsync(Guid id)
        {
            var request = await _requestProductRepository.GetRequestProductByRequestIdAsync(id);

            if (request == null)
            {
                throw new KeyNotFoundException($"RequestProduct with ID {id} not found.");
            }

            return new RequestProductDto
            {
                RequestProductId = request.RequestProductId,
                RequestCode = request.RequestCode,
                AgencyName = request.AgencyAccount?.AgencyName ?? "Unknown",
                AgencyId = request.AgencyId,
                //ApprovedBy = request.ApprovedBy,
                RequestStatus = request.RequestStatus,
                CreatedAt = request.CreatedAt,
                RequestProductDetails = request.RequestProductDetails.Select(d => new RequestProductDetailDto
                {
                    requestProductDetailId = d.RequestDetailId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.ProductName ?? "N/A",
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    UnitPrice = d.Price
                }).ToList()
            };
        }
        public async Task<List<RequestProductDto>> GetRequestProductsByAgencyIdAsync(long agencyId)
        {
            var requests = await _requestProductRepository.GetRequestProductAgencyIdAsync(agencyId);

            return requests.Select(rp => new RequestProductDto
            {
                RequestProductId = rp.RequestProductId,
                RequestCode = rp.RequestCode,
                AgencyName = rp.AgencyAccount?.AgencyName ?? "Unknown",
                AgencyId = rp.AgencyId,
                //ApprovedBy = rp.ApprovedBy,
                RequestStatus = rp.RequestStatus,
                CreatedAt = rp.CreatedAt,
                RequestProductDetails = rp.RequestProductDetails.Select(d => new RequestProductDetailDto
                {
                    requestProductDetailId = d.RequestDetailId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.ProductName ?? "N/A",
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    UnitPrice = d.Price
                }).ToList()
            }).ToList();
        }


        /*public async Task CreateRequestAsync(RequestProduct requestProduct, List<RequestProductDetail> requestDetails, Guid userId)
        {
            *//*long requestCodeID = Math.Abs(userId.GetHashCode()) % 1000000000;*//*

            Random random = new Random();
            string requestCode = await _requestProductRepository.GenerateRequestCodeAsync();


            // ✅ Lấy AgencyId từ UserId (GUID)
            var agencyId = await _userRepository.GetAgencyIdByUserId(userId);
            if (agencyId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy AgencyId từ User đang đăng nhập.");
            }


            var existingRequest = await _requestProductRepository.GetPendingRequestByAgencyAsync(agencyId.Value);

            // Tìm Order cũ từ RequestProduct nếu chưa Paid
            Order existingOrder = null;
            if (existingRequest != null)
            {
                existingOrder = await _orderRepository.GetOrderByRequestIdAsync(existingRequest.RequestProductId);
                if (existingOrder?.Status == "Paid")
                {
                    existingRequest = null; // Đơn cũ đã thanh toán rồi → bắt đầu lại
                    existingOrder = null;
                }
            }

            foreach (var newItem in requestDetails)
            {
                // ✅ Lấy thông tin sản phẩm từ Product
                //var product = await _productRepository.GetByIdAsync(newItem.ProductId);
                var product = await _productRepository.GetByIdAsync(newItem.ProductId, asNoTracking: true);

                if (product == null)
                {
                    throw new ArgumentException($"ProductId {newItem.ProductId} không tồn tại.");
                }

                // ✅ Kiểm tra số lượng tồn kho tính động
                var availableStock = await _productService.GetAvailableStockAsync(newItem.ProductId);
                if (newItem.Quantity > availableStock)
                {
                    throw new ArgumentException($"Sản phẩm {newItem.ProductId} không đủ hàng. Bạn chỉ có thể đặt tối đa {availableStock}.");
                }


                // ✅ Lấy giá từ Batch và tính giá bán (SellingPrice)
                var batch = await _batchRepository.GetLatestBatchByProductIdAsync(newItem.ProductId);
                if (batch == null)
                {
                    throw new ArgumentException($"Không tìm thấy lô hàng nào cho ProductId {newItem.ProductId}.");
                }

                decimal unitPrice = batch.SellingPrice ?? 0;

                if (existingRequest != null)
                {
                    var existingDetail = existingRequest.RequestProductDetails.FirstOrDefault(d => d.ProductId == newItem.ProductId);
                    if (existingDetail != null)
                    {
                        existingDetail.Quantity += newItem.Quantity;
                    }
                    else
                    {
                        existingRequest.RequestProductDetails.Add(new RequestProductDetail
                        {
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            Price = unitPrice,
                            Unit = newItem.Unit,
                        });
                    }
                }
                else
                {
                    if (requestProduct.RequestProductDetails == null)
                    {
                        requestProduct.RequestProductDetails = new List<RequestProductDetail>();
                    }

                    requestProduct.RequestProductDetails.Add(new RequestProductDetail
                    {
                        ProductId = newItem.ProductId,
                        Quantity = newItem.Quantity,
                        Unit = newItem.Unit,
                        Price = unitPrice
                    });
                }
            }

            if (existingRequest != null)
            {
                existingRequest.RequestCode = requestCode; // Gán requestCode cho đơn hàng đã tồn tại
                await _requestProductRepository.UpdateRequestAsync(existingRequest);
            }
            else
            {
                requestProduct.AgencyId = agencyId.Value; // Gán AgencyId từ User đăng nhập
                requestProduct.CreatedAt = DateTime.Now;
                requestProduct.RequestStatus = "Pending";
                await _requestProductRepository.AddRequestAsync(requestProduct);
            }
            requestProduct.RequestCode = requestCode;
            await _requestProductRepository.SaveChangesAsync();

            await ApproveRequestAsync(requestProduct.RequestProductId);


            *//*// Tạo object notification theo yêu cầu
            var agencyName = await _userRepository.GetAgencyNameByUserIdAsync(userId);
            var notification = new
            {
                title = "Sales", // Tiêu đề thông báo
                message = $"📦 Đơn hàng mới từ {agencyName}", // Nội dung thông báo
                payload = agencyName, // Bạn có thể thay bằng thông tin chi tiết nếu muốn
            };

            // Gửi thông báo qua SignalR
            await _hub.Clients.Group("4")
                .SendAsync("ReceiveNotification", notification);*//*


        }*/

        // ✅ Phiên bản CreateRequestAsync mới: xử lý gộp hoặc tạo mới RequestProduct + Order
        // ✅ Phiên bản CreateRequestAsync mới: xử lý gộp hoặc tạo mới RequestProduct + Order
        public async Task CreateRequestAsync(RequestProduct requestProduct, List<RequestProductDetail> requestDetails, Guid userId)
        {
            var agencyId = await _userRepository.GetAgencyIdByUserId(userId);
            if (agencyId == null)
                throw new UnauthorizedAccessException("Không tìm thấy AgencyId từ User.");
            Random random = new Random();
            string requestCode = await _requestProductRepository.GenerateRequestCodeAsync();
            // Kiểm tra đơn cũ chưa thanh toán
            var existingRequest = await _requestProductRepository.GetPendingRequestByAgencyAsync(agencyId.Value);
            Order existingOrder = null;
            if (existingRequest != null)
            {
                existingOrder = await _orderRepository.GetOrderByRequestIdAsync(existingRequest.RequestProductId);
                if (existingOrder?.Status == "Paid")
                {
                    existingRequest = null;
                    existingOrder = null;
                }
            }

            if (existingRequest != null && existingOrder != null)
            {
                // Gộp đơn hàng cũ
                foreach (var newItem in requestDetails)
                {
                    var existingDetail = existingRequest.RequestProductDetails.FirstOrDefault(d => d.ProductId == newItem.ProductId);
                    if (existingDetail != null)
                    {
                        existingDetail.Quantity += newItem.Quantity;
                    }
                    else
                    {
                        existingRequest.RequestProductDetails.Add(new RequestProductDetail
                        {
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            Price = newItem.Price,
                            Unit = newItem.Unit
                        });
                    }

                    var existingOrderDetail = await _orderRepository.GetOrderDetailAsync(existingOrder.OrderId, newItem.ProductId);
                    if (existingOrderDetail != null)
                    {
                        existingOrderDetail.Quantity += newItem.Quantity;
                        existingOrderDetail.TotalAmount = existingOrderDetail.Quantity * existingOrderDetail.UnitPrice;
                        await _orderRepository.UpdateOrderDetailAsync(existingOrderDetail);
                    }
                    else
                    {
                        var newDetail = new OrderDetail
                        {
                            OrderId = existingOrder.OrderId,
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            Unit = newItem.Unit,
                            UnitPrice = newItem.Price,
                            TotalAmount = newItem.Quantity * newItem.Price,
                            CreatedAt = DateTime.Now
                        };
                        await _orderRepository.AddOrderDetailAsync(new List<OrderDetail> { newDetail });
                    }
                }

                await _requestProductRepository.UpdateRequestAsync(existingRequest);
                await _orderRepository.UpdateOrderAsync(existingOrder);
            }
            else
            {
                // Tạo mới RequestProduct
                requestProduct.AgencyId = agencyId.Value;
                requestProduct.CreatedAt = DateTime.Now;
                requestProduct.RequestStatus = "Pending";
                requestProduct.RequestCode = await _requestProductRepository.GenerateRequestCodeAsync();

                await _requestProductRepository.AddRequestAsync(requestProduct);
                await _requestProductRepository.SaveChangesAsync();

                // Tạo mới Order
                var order = new Order
                {
                    OrderCode = requestCode,
                    OrderDate = DateTime.Now,
                    Status = "WaitPaid",
                    RequestId = requestProduct.RequestProductId,
                    Discount = 0,
                    FinalPrice = 0
                };

                await _orderRepository.AddOrderAsync(order);
                await _orderRepository.SaveChangesAsync();

                var orderDetails = requestProduct.RequestProductDetails.Select(d => new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    UnitPrice = d.Price,
                    TotalAmount = d.Quantity * d.Price,
                    CreatedAt = DateTime.Now
                }).ToList();

                await _orderRepository.AddOrderDetailAsync(orderDetails);
            }
        }


        // ✅ Giải pháp: cập nhật lại hàm ApproveRequestAsync thành ProcessOrderCreationAsync (nếu vẫn muốn giữ hàm này)
        public async Task ProcessOrderCreationAsync(Guid requestId)
        {
            string requestOrderCode = await _requestProductRepository.GenerateOrderCodeAsync();
            var requestProduct = await _requestProductRepository.GetRequestByIdAsync(requestId);
            if (requestProduct == null)
                throw new Exception("Request not found!");

            // Kiểm tra nếu Order đã tồn tại và đã thanh toán thì không xử lý nữa
            var existingOrder = await _orderRepository.GetOrderByRequestIdAsync(requestId);
            if (existingOrder != null && existingOrder.Status == "Paid")
                throw new Exception("Order already paid. Cannot process again.");

            // Nếu chưa có Order hoặc Order chưa thanh toán => tạo hoặc cập nhật
            Order order;
            bool isNewOrder = false;

            if (existingOrder == null)
            {
                order = new Order
                {
                    OrderCode = requestOrderCode,
                    OrderDate = DateTime.Now,
                    Status = "WaitPaid",
                    RequestId = requestId,
                    Discount = 0,
                    FinalPrice = 0
                };

                await _orderRepository.AddOrderAsync(order);
                await _orderRepository.SaveChangesAsync();
                isNewOrder = true;
            }
            else
            {
                order = existingOrder;
                order.OrderDate = DateTime.Now;
            }

            decimal finalPrice = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var detail in requestProduct.RequestProductDetails)
            {
                var unitPrice = detail.Price;
                var totalAmount = detail.Quantity * unitPrice;

                var existingDetail = !isNewOrder
                    ? await _orderRepository.GetOrderDetailAsync(order.OrderId, detail.ProductId)
                    : null;

                if (existingDetail != null)
                {
                    existingDetail.Quantity = detail.Quantity;
                    existingDetail.UnitPrice = unitPrice;
                    existingDetail.TotalAmount = totalAmount;
                    existingDetail.Unit = detail.Unit;
                    existingDetail.CreatedAt = DateTime.Now;

                    await _orderRepository.UpdateOrderDetailAsync(existingDetail);
                }
                else
                {
                    orderDetails.Add(new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        UnitPrice = unitPrice,
                        TotalAmount = totalAmount,
                        Unit = detail.Unit,
                        CreatedAt = DateTime.Now
                    });
                }

                finalPrice += totalAmount;
            }

            if (orderDetails.Any())
            {
                await _orderRepository.AddOrderDetailAsync(orderDetails);
            }

            order.FinalPrice = finalPrice;
            await _orderRepository.UpdateOrderAsync(order);
            await _orderRepository.SaveChangesAsync();
        }

        /*public async Task ApproveRequestAsync(Guid requestId)
        {
            try
            {
                var requestProduct = await _requestProductRepository.GetRequestByIdAsync(requestId);
                if (requestProduct == null) throw new Exception("Request not found!");

                string requestOrderCode = await _requestProductRepository.GenerateOrderCodeAsync(); 

                if (requestProduct.RequestStatus == "Approved")
                {
                    throw new Exception("This request has already been approved and cannot be approved again.");
                }

                // Cập nhật trạng thái RequestProduct
                //requestProduct.ApprovedBy = approvedBy;
                requestProduct.RequestStatus = "Approved";
                requestProduct.UpdatedAt = DateTime.Now;

                await _requestProductRepository.UpdateRequestAsync(requestProduct);
                await _requestProductRepository.SaveChangesAsync();

                var existingOrder = await _orderRepository.GetOrderByRequestIdAsync(requestId);

                Order order;
                bool isNewOrder = false;

                if (existingOrder == null || existingOrder.Status != "WaitPaid")
                {
                    // ✅ Tạo mới
                    order = new Order
                    {
                        OrderCode = requestOrderCode,
                        OrderDate = DateTime.Now,
                        //SalesAgentId = approvedBy,
                        Status = "WaitPaid",
                        RequestId = requestId,
                        Discount = 0,
                        FinalPrice = 0
                    };

                    await _orderRepository.AddOrderAsync(order);
                    await _orderRepository.SaveChangesAsync();
                    isNewOrder = true;

                    foreach (var detail in requestProduct.RequestProductDetails)
                    {
                        await _inventoryService.DeductStockByWarehouseProductAsync(order.OrderId, detail.ProductId, detail.Quantity);

                        // ✅ Cập nhật lại tồn kho sau khi trừ
                        //await _productService.RecalculateAvailableStockAsync(detail.ProductId);
                    }
                }
                else
                {
                    // ✅ Cập nhật đơn cũ
                    order = existingOrder;
                    order.OrderDate = DateTime.Now;
                }

                decimal finalPrice = 0;
                var orderDetails = new List<OrderDetail>();

                foreach (var detail in requestProduct.RequestProductDetails)
                {
                    var unitPrice = detail.Price;
                    var totalAmount = detail.Quantity * unitPrice;

                    var existingDetail = !isNewOrder
                        ? await _orderRepository.GetOrderDetailAsync(order.OrderId, detail.ProductId)
                        : null;

                    if (existingDetail != null)
                    {
                        // ✅ Cập nhật nếu đã có
                        existingDetail.Quantity = detail.Quantity;
                        existingDetail.UnitPrice = unitPrice;
                        existingDetail.TotalAmount = totalAmount;
                        existingDetail.Unit = detail.Unit;
                        existingDetail.CreatedAt = DateTime.Now;

                        await _orderRepository.UpdateOrderDetailAsync(existingDetail);
                    }
                    else
                    {
                        // ✅ Thêm mới
                        var newDetail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            UnitPrice = unitPrice,
                            TotalAmount = totalAmount,
                            Unit = detail.Unit,
                            CreatedAt = DateTime.Now
                        };

                        orderDetails.Add(newDetail);
                    }

                    finalPrice += totalAmount;
                }

                if (orderDetails.Any())
                {
                    await _orderRepository.AddOrderDetailAsync(orderDetails);
                }

                order.FinalPrice = finalPrice;
                await _orderRepository.UpdateOrderAsync(order);
                await _orderRepository.SaveChangesAsync();


                *//*var notification = new
                {
                    title = "Agency", // Tiêu đề thông báo
                    message = $"✅ Đơn hàng {requestProduct.RequestCode} đã được duyệt!", // Nội dung thông báo
                    payload = requestProduct.RequestCode // Có thể thay bằng dữ liệu chi tiết nếu cần
                };

                // Gửi thông báo qua SignalR cho AGENCY
                await _hub.Clients.Group("2")
                    .SendAsync("ReceiveNotification", notification);*//*

            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Database update failed: {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }
        }*/


        public async Task<bool> CancelRequestAsync(Guid requestId, long approvedBy)
        {
            var requestProduct = await _requestProductRepository.GetRequestByIdAsync(requestId);

            if (requestProduct == null)
                throw new Exception("RequestProduct not found!");

            if (requestProduct.RequestStatus == "Canceled")
                throw new Exception("RequestProduct is already canceled!");

            if (requestProduct.RequestStatus == "Approved")
                throw new Exception("Cannot cancel an approved request!");

            requestProduct.RequestStatus = "Canceled";
            //requestProduct.ApprovedBy = approvedBy;
            requestProduct.UpdatedAt = DateTime.Now;

            await _requestProductRepository.UpdateRequestAsync(requestProduct);
            await _requestProductRepository.SaveChangesAsync();

            return true;
        }

    }
}
