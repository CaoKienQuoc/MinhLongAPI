using BusinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repo.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.Repository
{
    public class RequestExportRepository : IRequestExportRepository
    {
        private readonly MinhLongDbContext _context;

        public RequestExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<RequestExport>> GetAllRequestExportsAsync()
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                    .ThenInclude(red => red.Product)
                .Include(re => re.RequestedByAgency)             // Lấy tên Agency
                .OrderByDescending(re => re.RequestDate)
                .ToListAsync();
        }

        public async Task<List<RequestExport>> GetRequestExportsBySalesUserIdAsync(Guid salesUserId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                    .ThenInclude(red => red.Product)
                .Include(re => re.RequestedByAgency)
                    .ThenInclude(aa => aa.ManagedByEmployee)
                        .ThenInclude(emp => emp.User)
                .Include(re => re.Order)
                .ThenInclude(red => red.TemporaryStockExports)
                .ThenInclude(red => red.Warehouse)
                .Where(re => re.RequestedByAgency.ManagedByEmployee != null &&
                             re.RequestedByAgency.ManagedByEmployee.UserId == salesUserId)
                .OrderByDescending(re => re.RequestDate)
                .ToListAsync();
        }

        public async Task<RequestExport?> GetRequestExportByIdAsync(int requestId, Guid salesUserId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                    .ThenInclude(red => red.Product)
                .Include(re => re.RequestedByAgency)
                    .ThenInclude(aa => aa.ManagedByEmployee)
                        .ThenInclude(emp => emp.User)
                .Include(re => re.Order)
                .ThenInclude(red => red.TemporaryStockExports)
                .ThenInclude(red => red.Warehouse)
                .Where(re => re.RequestExportId == requestId &&
                             re.RequestedByAgency.ManagedByEmployee != null &&
                             re.RequestedByAgency.ManagedByEmployee.UserId == salesUserId)
                .FirstOrDefaultAsync();
        }




        public async Task AddExportAsync(RequestExport export)
        {
            await _context.RequestExports.AddAsync(export);
        }

        public async Task AddExportDetailsAsync(List<RequestExportDetail> exportDetails)
        {
            await _context.RequestExportDetails.AddRangeAsync(exportDetails);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<RequestExport> GetRequestExportById(int requestId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                .ThenInclude(red => red.Product)
                .FirstOrDefaultAsync(r => r.RequestExportId == requestId);
             
        }

        public async Task UpdateExportAsync(RequestExport export)
        {
            _context.RequestExports.Update(export);
            await Task.CompletedTask; // hoặc bạn có thể không await gì vì update chỉ cập nhật tracking entity
        }


        public async Task<RequestExport> GetRequestExportByIdAsync(int requestExportId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                .FirstOrDefaultAsync(re => re.RequestExportId == requestExportId);
        }

        public async Task UpdateRequestExportAsync(RequestExport requestExport)
        {
            _context.RequestExports.Update(requestExport);
            await Task.CompletedTask;
        }

        public async Task<Guid?> GetOrderIdByRequestExportIdAsync(int requestExportId)
        {
            return await _context.RequestExports
                .Where(r => r.RequestExportId == requestExportId)
                .Select(r => r.OrderId)
                .FirstOrDefaultAsync();
        }
    }


}
