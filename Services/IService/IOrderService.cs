using BusinessObject.DTO.Dashboard;
using BusinessObject.DTO.Order;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IOrderService
    {
        //Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<List<OrderDto>> GetAllOrdersAsync();

        Task<List<OrderDto>> GetAllOrdersBySalesUserAsync(Guid salesUserId);

        //Task<Order> GetOrderByIdAsync(Guid orderId);
        Task<OrderDto> GetOrderByIdAsync(Guid orderId);

        Task<OrderDto> GetOrderByIdBySalesUserAsync(Guid orderId, Guid salesUserId);

        Task<bool> CancelOrderAsync(Guid orderId);

        //Task<List<Order>> GetOrdersByAgencyIdAsync(long agencyId);
        Task<List<OrderDto>> GetOrdersByAgencyIdAsync(long agencyId);

        Task<bool> ProcessPaymentAsync(Guid orderId);

        Task<Order> GetOrderByOrderCodeAsync(string orderCode);


        Task<List<object>> GetOrderStatusCountsAsync(Guid userId);
        Task<List<object>> GetDailyRevenueAsync();
        Task<List<object>> GetTopSellingProductsAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<List<object>> GetMonthlyOrderStatsAsync();
        Task<int> GetTodayOrderCountAsync();
        Task<int> GetThisMonthOrderCountAsync();

        Task<int> GetOrderCountManagedBySalesAsync(Guid salesUserId);
        Task<decimal> GetTotalRevenueManagedBySalesAsync(Guid salesUserId);




        Task<decimal> GetTodayRevenueByUserIdAsync(Guid userId);
        Task<decimal> GetThisMonthRevenueByUserIdAsync(Guid userId);
        Task<decimal> GetTotalRevenueByUserIdAsync(Guid userId);

        Task<List<object>> GetMonthlyExportedOrderStatsAsync();

        Task<Dictionary<Guid, decimal>> GetImportCostForSalesOrdersAsync(Guid salesUserId);
        Task<List<object>> GetProfitStatsForSalesOrdersAsync(Guid salesUserId);
        Task<SalesDashboardStatsDto> GetSalesDashboardAsync(Guid salesUserId, DateTime? fromDate, DateTime? toDate);



    }
}
