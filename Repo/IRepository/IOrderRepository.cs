using BusinessObject.DTO.Dashboard;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IOrderRepository
    {

        //Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<decimal> GetAgencyDiscountAsync(long agencyId);
        Task<List<Order>> GetAllOrdersAsync();
        Task AddOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task SaveChangesAsync();

        Task AddOrderDetailAsync(List<OrderDetail> orderDetails);

        Task<Order> GetOrderByIdAsync(Guid orderId);
        Task<RequestProduct> GetRequestProductByOrderAsync(Guid orderId);
        Task SaveAsync();

        Task<int> GetTotalQuantityByOrderIdAsync(Guid orderId);

        Task<List<Order>> GetOrdersByAgencyIdAsync(long agencyId);

        Task<Order?> SingleOrDefaultAsync(Expression<Func<Order, bool>> predicate);

        Task<Order> GetOrderByOrderCodeAsync(string orderCode);

        Task<Order?> GetOrderByRequestIdAsync(Guid requestId);
        Task<OrderDetail?> GetOrderDetailAsync(Guid orderId, long productId);
        Task UpdateOrderDetailAsync(OrderDetail detail);
        Task<string> GenerateRequestExportCodeAsync();

        Task<int> GetOrderCountManagedBySalesAsync(Guid salesUserId);
        Task<decimal> GetTotalPaymentAmountManagedBySalesAsync(Guid salesUserId);

        Task<Order> GetOrderWithDetailsAsync(Guid orderId); // ✅ đổi kiểu orderId

        Task<OrderDetail> GetOrderDetailByIdAsync(Guid orderDetailId);
        Task<List<object>> GetMonthlyExportedOrderStatsAsync();

        Task<List<Guid>> GetOrderIdsManagedBySalesAsync(Guid salesUserId);
        Task<Dictionary<Guid, decimal>> GetImportCostPerOrderFromTemporaryStockExportAsync(List<Guid> orderIds);

        Task<Dictionary<Guid, decimal>> GetRevenuePerOrderAsync(List<Guid> orderIds);
        Task<List<Order>> GetOrdersManagedBySalesAsync(Guid salesUserId, DateTime? fromDate, DateTime? toDate);

        Task<decimal?> GetUnitPriceAsync(Guid orderId, long productId);


    }
}
