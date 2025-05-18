using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;

namespace Repo.Repository
{
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly MinhLongDbContext _context;
        public ReturnRequestRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnRequest> CreateAsync(ReturnRequest request)
        {
            _context.ReturnRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<ReturnRequest> GetByIdAsync(Guid id)
            => await _context.ReturnRequests
                            .Include(r => r.Details)
                            .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

        public async Task<List<ReturnRequest>> GetPendingApprovalAsync()
            => await _context.ReturnRequests
                             .Where(r => r.Status == "Pending")
                             .ToListAsync();

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            var request = await _context.ReturnRequests.FindAsync(id);
            if (request == null) throw new Exception("Không tìm thấy yêu cầu trả hàng.");
            request.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task<ReturnRequest> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                    .ThenInclude(d => d.OrderDetail)
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        public async Task<List<ReturnRequestImage>> AddRangeAsync(List<ReturnRequestImage> images)
        {
            _context.ReturnRequestImages.AddRange(images);
            await _context.SaveChangesAsync();
            return images;
        }

        public async Task<List<ReturnRequest>> GetAllAsync()
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                .Include(r => r.Images)
                .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                    .Include(r => r.Order)
                     .ThenInclude(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(aa => aa.ManagedByEmployee) // thêm dòng này
                            .ThenInclude(e => e.User) // để có UserId của nhân viên
                            .OrderByDescending(r => r.CreatedAt) // ✅ Sắp xếp theo ngày tạo mới nhất
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetByIdWithAllDetailsAsync(Guid id)
        {
            return await _context.ReturnRequests
               .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                .Include(r => r.Images)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                    .Include(r => r.Order)
                     .ThenInclude(o => o.RequestProduct)
                    .ThenInclude(rp => rp.AgencyAccount)
                        .ThenInclude(aa => aa.ManagedByEmployee) // thêm dòng này
                            .ThenInclude(e => e.User) // để có UserId của nhân viên
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        public async Task<List<ReturnRequest>> GetApprovedAsync()
        {
            return await _context.ReturnRequests
                .Where(r => r.Status == "Approved")
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetApprovedByIdAsync(Guid id)
        {
            return await _context.ReturnRequests
                .Where(r => r.ReturnRequestId == id && r.Status == "Approved")
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetTotalReturnedQuantityAsync(Guid orderDetailId)
        {
            return await _context.ReturnRequestDetails
                .Where(d => d.OrderDetailId == orderDetailId)
                .SumAsync(d => (int?)d.QuantityReturned) ?? 0;
        }
        public async Task<IEnumerable<ReturnRequest>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                .Include(r => r.Images)
                .Where(r => r.CreatedByUserId == userId)
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetByIdAndUserIdAsync(Guid returnRequestId, Guid userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId && r.CreatedByUserId == userId);
        }
        public async Task<string> GenerateRequestReturnCodeAsync()
        {
            var today = DateTime.Now.Date;
            int countToday = await _context.RequestProducts
                .Where(r => r.CreatedAt.Date == today)
                .CountAsync();

            string datePart = today.ToString("yyyyMMdd");
            string requestCode = $"RT-{datePart}-{(countToday + 1):D3}";

            return requestCode;
        }
        public async Task<string> GenerateWarehouseReturnCodeAsync()
        {
            var today = DateTime.Now.Date;
            int countToday = await _context.RequestProducts
                .Where(r => r.CreatedAt.Date == today)
                .CountAsync();

            string datePart = today.ToString("yyyyMMdd");
            string requestCode = $"WR{datePart}-{(countToday + 1):D3}";

            return requestCode;
        }

        public async Task UpdateAsync(ReturnRequest request)
        {
            _context.ReturnRequests.Update(request);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message;
                // Log chi tiết lỗi ra console hoặc logger
                Console.WriteLine("DbUpdateException: " + ex.Message);
                Console.WriteLine("InnerException: " + innerMessage);

                // Bạn có thể log ra logger nếu dùng ILogger
                // _logger.LogError(ex, "Lỗi SaveChanges: {Message}", innerMessage);

                throw new Exception("Lỗi khi lưu dữ liệu: " + innerMessage, ex);
            }
        }

        public async Task AddDetailsAsync(IEnumerable<ReturnRequestDetail> details)
        {
            _context.ReturnRequestDetails.AddRange(details);
        }

        public async Task<List<Guid>> GetDetailIdsByReturnRequestIdAsync(Guid returnRequestId)
        {
            if (returnRequestId == Guid.Empty)
                throw new ArgumentException("ReturnRequestId không hợp lệ.");

            return await _context.ReturnRequestDetails
                .Where(d => d.ReturnRequestId == returnRequestId)
                .Select(d => d.ReturnRequestDetailId)
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetByOrderAndProductAsync(Guid orderId, long productId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.Details.Any(d => d.ProductId == productId));
        }

        public async Task<ReturnRequest?> GetLatestReturnRequestByOrderIdAsync(Guid orderId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details) // nếu bạn cần load danh sách chi tiết
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ReturnRequest> UpdateReturnAsync(ReturnRequest request)
        {
            _context.ReturnRequests.Update(request);
            await _context.SaveChangesAsync();
            return request; // ✅ Trả về request sau khi cập nhật
        }

    }

}
