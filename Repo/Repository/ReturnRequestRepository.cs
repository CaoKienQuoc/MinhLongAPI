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
                            .ThenInclude(d => d.Images)
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
                    .ThenInclude(d => d.Images)
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
                    .ThenInclude(d => d.Images)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetByIdWithAllDetailsAsync(Guid id)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Images)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        public async Task<List<ReturnRequest>> GetApprovedAsync()
        {
            return await _context.ReturnRequests
                .Where(r => r.Status == "Approved")
                .Include(r => r.Details)
                .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Images)
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
                    .ThenInclude(d => d.Images)
                    .Include(r => r.Order)
                    .ThenInclude(r => r.RequestProduct)
                    .ThenInclude(r => r.AgencyAccount)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ReturnRequest>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Images)
                .Where(r => r.CreatedByUserId == userId)
                .ToListAsync();
        }

        public async Task<ReturnRequest> GetByIdAndUserIdAsync(Guid returnRequestId, Guid userId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Images)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId && r.CreatedByUserId == userId);
        }
    }

}
