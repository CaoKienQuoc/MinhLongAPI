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

        // ✅ Phiên bản hoàn chỉnh: bám theo logic cũ, thay thế ApproveRequestAsync bằng ProcessOrderCreationAsync
        // ✅ Phiên bản hoàn chỉnh: xử lý cộng dồn đơn hàng và kho tạm đúng logic
        public async Task CreateRequestAsync(RequestProduct requestProduct, List<RequestProductDetail> requestDetails, Guid userId)
        {
            string requestCode = await _requestProductRepository.GenerateRequestCodeAsync();

            if (requestDetails == null || !requestDetails.Any())
                throw new ArgumentException("Danh sách sản phẩm không được rỗng.");

            var agencyId = await _userRepository.GetAgencyIdByUserId(userId);
            if (agencyId == null)
                throw new UnauthorizedAccessException("Không tìm thấy AgencyId từ User đang đăng nhập.");

            var existingRequest = await _requestProductRepository.GetPendingRequestByAgencyAsync(agencyId.Value);

            
            Order existingOrder = null;

            if (existingRequest != null)
            {
                existingOrder = await _orderRepository.GetOrderByRequestIdAsync(existingRequest.RequestProductId);
                if (existingOrder?.Status == "Paid" || existingOrder?.Status == "Canceled" || existingRequest.RequestStatus == "Canceled")
                {
                    existingRequest = null;
                    existingOrder = null;
                }
            }

            var deltaRequests = new List<(long ProductId, long DeltaQuantity)>();

            foreach (var newItem in requestDetails)
            {
                var product = await _productRepository.GetByIdAsync(newItem.ProductId, asNoTracking: true);
                if (product == null)
                    throw new ArgumentException($"ProductId {newItem.ProductId} không tồn tại.");

                var availableStock = await _productService.GetAvailableStockAsync(newItem.ProductId);
                if (newItem.Quantity > availableStock)
                    throw new ArgumentException($"Sản phẩm {newItem.ProductId} không đủ hàng. Bạn chỉ có thể đặt tối đa {availableStock}.");

                var batch = await _batchRepository.GetLatestBatchByProductIdAsync(newItem.ProductId);
                if (batch == null)
                    throw new ArgumentException($"Không tìm thấy lô hàng nào cho ProductId {newItem.ProductId}.");

                decimal unitPrice = batch.SellingPrice ?? 0;

                if (existingRequest != null)
                {
                    var existingDetail = existingRequest.RequestProductDetails.FirstOrDefault(d => d.ProductId == newItem.ProductId);
                    if (existingDetail != null)
                    {
                        existingDetail.Quantity += newItem.Quantity;
                        deltaRequests.Add((newItem.ProductId, newItem.Quantity));
                    }
                    else
                    {
                        existingRequest.RequestProductDetails.Add(new RequestProductDetail
                        {
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            Price = unitPrice,
                            Unit = newItem.Unit
                        });
                        deltaRequests.Add((newItem.ProductId, newItem.Quantity));
                    }
                }
                else
                {
                    requestProduct.RequestProductDetails ??= new List<RequestProductDetail>();
                    requestProduct.RequestProductDetails.Add(new RequestProductDetail
                    {
                        ProductId = newItem.ProductId,
                        Quantity = newItem.Quantity,
                        Unit = newItem.Unit,
                        Price = unitPrice
                    });
                    deltaRequests.Add((newItem.ProductId, newItem.Quantity));
                }
            }

            Guid requestProductId;

            if (existingRequest != null)
            {
                existingRequest.RequestCode = requestCode;
                await _requestProductRepository.UpdateRequestAsync(existingRequest);
                await _requestProductRepository.SaveChangesAsync();
                requestProductId = existingRequest.RequestProductId;
            }
            else
            {
                requestProduct.AgencyId = agencyId.Value;
                requestProduct.CreatedAt = DateTime.Now;
                requestProduct.RequestStatus = "Pending";
                requestProduct.RequestCode = requestCode;

                await _requestProductRepository.AddRequestAsync(requestProduct);
                await _requestProductRepository.SaveChangesAsync();
                requestProductId = requestProduct.RequestProductId;
            }

            // ✅ Gọi xử lý đơn hàng (không trừ kho trong hàm này)
            await ProcessOrderCreationAsync(requestProductId);

            // ✅ Lấy lại đơn hàng để biết OrderId sau khi tạo
            var order = await _orderRepository.GetOrderByRequestIdAsync(requestProductId);

            // ✅ Trừ kho đúng số lượng mới thêm
            foreach (var delta in deltaRequests)
            {
                await _inventoryService.DeductStockByWarehouseProductAsync(order.OrderId, delta.ProductId, delta.DeltaQuantity);
            }
        }

        public async Task ProcessOrderCreationAsync(Guid requestId)
        {
            string requestOrderCode = await _requestProductRepository.GenerateOrderCodeAsync();
            var requestProduct = await _requestProductRepository.GetRequestByIdAsync(requestId);
            if (requestProduct == null)
                throw new Exception("Request not found!");

            var existingOrder = await _orderRepository.GetOrderByRequestIdAsync(requestId);
            if (existingOrder != null && existingOrder.Status == "Paid")
                return;

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
                await _orderRepository.AddOrderDetailAsync(orderDetails);

            order.FinalPrice = finalPrice;
            await _orderRepository.UpdateOrderAsync(order);
            await _orderRepository.SaveChangesAsync();
        }




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
