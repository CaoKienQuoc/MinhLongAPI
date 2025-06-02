using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly MinhLongDbContext _context;

        public OrderRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<RequestExport>> GetExportsManagedByEmployeeAsync(long employeeId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                    .ThenInclude(red => red.Product)
                .Include(re => re.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                            .ThenInclude(aa => aa.ManagedByEmployee)
                .Where(re => re.Order.RequestProduct.AgencyAccount.ManagedByEmployeeId == employeeId)
                .ToListAsync();
        }


        public async Task<decimal> GetAgencyDiscountAsync(long agencyId)
        {
            var latestLevel = await _context.AgencyAccountLevels
                .Where(aal => aal.AgencyId == agencyId)
                .OrderByDescending(aal => aal.ChangeDate)
                .Select(aal => aal.Level)
                .FirstOrDefaultAsync();

            return latestLevel?.DiscountPercentage ?? 0;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(aa => aa.ManagedByEmployee) // thêm dòng này
                            .ThenInclude(e => e.User) // để có UserId của nhân viên
                .ToListAsync();
        }



        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddOrderDetailAsync(List<OrderDetail> orderDetails) // ✅ Thêm phương thức này
        {
            await _context.OrderDetails.AddRangeAsync(orderDetails); // 🔹 Thêm danh sách `OrderDetail` cùng lúc
        }

        /*public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }*/

        public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(aa => aa.ManagedByEmployee) // thêm dòng này
                            .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }


        public async Task<RequestProduct> GetRequestProductByOrderAsync(Guid orderId)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) return null;

            return await _context.RequestProducts
                .FirstOrDefaultAsync(rp => rp.RequestProductId == order.RequestId);
        }
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetTotalQuantityByOrderIdAsync(Guid orderId)
        {
            return await _context.OrderDetails
                                 .Where(od => od.OrderId == orderId)
                                 .SumAsync(od => od.Quantity);
        }

        /*public async Task<List<Order>> GetOrdersByAgencyIdAsync(long agencyId)
        {
            return await _context.Orders
                .Include(o => o.RequestProduct)
                .Where(o => o.RequestProduct.AgencyId == agencyId)
                .ToListAsync();
        }*/

        public async Task<List<Order>> GetOrdersByAgencyIdAsync(long agencyId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                    .ThenInclude(aa => aa.AgencyAccountLevels)
                .Where(o => o.RequestProduct.AgencyId == agencyId)
                .ToListAsync();
        }


        public async Task<Order?> SingleOrDefaultAsync(Expression<Func<Order, bool>> predicate)
        {
            return await _context.Orders.SingleOrDefaultAsync(predicate);
        }

        public async Task<Order> GetOrderByOrderCodeAsync(string orderCode)
        {
            return await _context.Orders.SingleOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        public async Task<Order?> GetOrderByRequestIdAsync(Guid requestId)
        {
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.RequestId == requestId);
        }

        public async Task<OrderDetail?> GetOrderDetailAsync(Guid orderId, long productId)
        {
            return await _context.OrderDetails
                .FirstOrDefaultAsync(od => od.OrderId == orderId && od.ProductId == productId);
        }

        public async Task UpdateOrderDetailAsync(OrderDetail detail)
        {
            _context.OrderDetails.Update(detail);
            await Task.CompletedTask;
        }

        public async Task<string> GenerateRequestExportCodeAsync()
        {
            var today = DateTime.Now.Date;
            int countToday = await _context.RequestProducts
                .Where(r => r.CreatedAt.Date == today)
                .CountAsync();

            string datePart = today.ToString("yyyyMMdd");
            string requestCode = $"RQE{datePart}-{(countToday + 1):D3}";

            return requestCode;
        }

        public async Task<decimal?> GetCreditLimitByUserIdAsync(Guid userId)
        {
            return await _context.AgencyAccounts
                .Where(aa => aa.UserId == userId)
                .SelectMany(aa => aa.AgencyAccountLevels)
                .OrderByDescending(aal => aal.ChangeDate)
                .Select(aal => aal.Level.CreditLimit)
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetPaymentTermByUserIdAsync(Guid userId)
        {
            return await _context.AgencyAccounts
                .Where(aa => aa.UserId == userId)
                .SelectMany(aa => aa.AgencyAccountLevels)
                .OrderByDescending(aal => aal.ChangeDate)
                .Select(aal => aal.Level.PaymentTerm)
                .FirstOrDefaultAsync();
        }


        public async Task<int> GetOrderCountManagedBySalesAsync(Guid salesUserId)
        {
            return await _context.Orders
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(aa => aa.ManagedByEmployee)
                .Where(o => o.RequestProduct.AgencyAccount.ManagedByEmployee.User.UserId == salesUserId
                            && (o.Status == "Paid" || o.Status == "WaitingDelivery" || o.Status == "Exported"))
                .CountAsync();
        }

        public async Task<decimal> GetTotalPaymentAmountManagedBySalesAsync(Guid salesUserId)
        {
            return await _context.PaymentHistories
                .Include(ph => ph.Order)
                    .ThenInclude(o => o.RequestProduct)
                        .ThenInclude(rp => rp.AgencyAccount)
                            .ThenInclude(a => a.ManagedByEmployee)
                .Where(ph => ph.Order.RequestProduct.AgencyAccount.ManagedByEmployee.User.UserId == salesUserId)
                .SumAsync(ph => ph.PaymentAmount);
        }

        public async Task<Order> GetOrderWithDetailsAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<OrderDetail> GetOrderDetailByIdAsync(Guid orderDetailId)
        {
            var orderDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(x => x.OrderDetailId == orderDetailId);

            if (orderDetail == null)
                throw new Exception($"Không tìm thấy OrderDetail với ID: {orderDetailId}");

            return orderDetail;
        }

        public async Task<List<object>> GetMonthlyExportedOrderStatsAsync()
        {
            var currentYear = DateTime.Now.Year;

            return await _context.Orders
                .Where(o => o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    ExportedOrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.FinalPrice),
                    TotalProductSold = g.Sum(o => o.OrderDetails.Sum(d => d.Quantity))
                })
                .OrderBy(x => x.Month)
                .Select(x => (object)x)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetOrderIdsManagedBySalesAsync(Guid salesUserId)
        {
            return await _context.Orders
                .Where(o => o.RequestProduct.AgencyAccount.ManagedByEmployee.User.UserId == salesUserId &&
                    (o.Status == "Paid" || o.Status == "WaitingDelivery" || o.Status == "Exported"))
                .Select(o => o.OrderId)
                .ToListAsync();
        }

        // Tính số tiền nhập hàng cho từng đơn hàng
        public async Task<Dictionary<Guid, decimal>> GetImportCostPerOrderFromTemporaryStockExportAsync(List<Guid> orderIds)
        {
            var temporaryStockExports = await _context.TemporaryStockExports
                .Where(tse => orderIds.Contains(tse.OrderId))
                .Include(tse => tse.Batch) // Include Batch để lấy thông tin giá nhập
                .ToListAsync();

            // Tính tiền nhập kho cho từng đơn hàng
            var importCostsPerOrder = temporaryStockExports
                .GroupBy(tse => tse.OrderId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(tse => tse.Quantity * tse.Batch.UnitCost)
                );

            return importCostsPerOrder;
        }


        public async Task<Dictionary<Guid, decimal>> GetRevenuePerOrderAsync(List<Guid> orderIds)
        {
            var orders = await _context.Orders
                .Include(o => o.PaymentHistories)
                .Where(o => orderIds.Contains(o.OrderId))
                .ToListAsync();

            var revenuePerOrder = orders
                .ToDictionary(
                    order => order.OrderId,
                    order => order.FinalPrice
                );

            return revenuePerOrder;
        }

        public async Task<List<Order>> GetOrdersManagedBySalesAsync(Guid salesUserId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(a => a.ManagedByEmployee)
                .Where(o => o.RequestProduct.AgencyAccount.ManagedByEmployee.User.UserId == salesUserId &&
                            (o.Status == "Paid" || o.Status == "WaitingDelivery" || o.Status == "Exported"));

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate.Date <= toDate.Value.Date);

            return await query.ToListAsync();
        }

        public async Task<decimal?> GetUnitPriceAsync(Guid orderId, long productId)
        {
            return await _context.OrderDetails
                .Where(od => od.OrderId == orderId && od.ProductId == productId)
                .Select(od => (decimal?)od.UnitPrice)
                .FirstOrDefaultAsync();
        }
    }
}
