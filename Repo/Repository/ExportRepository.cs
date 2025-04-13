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
    public class ExportRepository : IExportRepository
    {
        private readonly MinhLongDbContext _context;

        public ExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<List<RequestExport>> GetAllRequestExportsAsync()
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                    .ThenInclude(red => red.Product)
                .Include(re => re.RequestedByAgency)             // Lấy tên Agency
                .Include(re => re.ApprovedByEmployee)            // 👈 Lấy Employee để truy cập FullName
                .ToListAsync();
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
    }


}
