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

        /*public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.
                Include(o => o.OrderDetails).
                ToListAsync();
        }*/

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
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
                .Include(o => o.RequestProduct)
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

    }
}
