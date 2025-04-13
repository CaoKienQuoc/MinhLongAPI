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
    public class WarehouseRequestExportRepository : IWarehouseRequestExportRepository
    {
        private readonly MinhLongDbContext _context;

        public WarehouseRequestExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<RequestExport> GetRequestExportByIdAsync(int requestExportId)
        {
            return await _context.RequestExports
                .Include(re => re.RequestExportDetails)
                .FirstOrDefaultAsync(re => re.RequestExportId == requestExportId);
        }

        public async Task<WarehouseRequestExport> CreateWarehouseRequestExportAsync(long warehouseId, int requestExportId)
        {
            // Dùng phương thức có sẵn trong repository
            var requestExport = await GetRequestExportByIdAsync(requestExportId);

            if (requestExport == null || requestExport.RequestExportDetails == null || !requestExport.RequestExportDetails.Any())
            {
                return null;
            }

            /*// ✅ Chặn tạo lại nếu đã REQUESTED
            if (requestExport.Status == "REQUESTED" || requestExport.Status == "APPROVED")
            {
                throw new InvalidOperationException("This request has already been assigned to a warehouse.");
            }

            var warehouseRequests = requestExport.RequestExportDetails.Select(detail => new WarehouseRequestExport
            {
                WarehouseId = warehouseId,
                RequestExportId = requestExportId,
                ProductId = detail.ProductId,
                QuantityRequested = detail.RequestedQuantity,
                RemainingQuantity = detail.RequestedQuantity,
                Status = "PENDING"
            }).ToList();

            await _context.WarehouseRequestExports.AddRangeAsync(warehouseRequests);

            // ✅ Cập nhật trạng thái sang REQUESTED
            requestExport.Status = "REQUESTED";
            _context.RequestExports.Update(requestExport);

            await _context.SaveChangesAsync();

            return warehouseRequests.FirstOrDefault();*/

            // ✅ Chặn tạo lại nếu đã REQUESTED
            if (requestExport.Status == "Requested" || requestExport.Status == "Approved")
            {
                throw new InvalidOperationException("This request has already been assigned to a warehouse.");
            }

            var warehouseRequests = requestExport.RequestExportDetails.Select(detail => new WarehouseRequestExport
            {
                WarehouseId = warehouseId,
                RequestExportId = requestExportId,
                ProductId = detail.ProductId,
                QuantityRequested = detail.RequestedQuantity,
                RemainingQuantity = detail.RequestedQuantity,
                Status = "PENDING"
            }).ToList();

            await _context.WarehouseRequestExports.AddRangeAsync(warehouseRequests);

            // ✅ Cập nhật trạng thái sang REQUESTED
            requestExport.Status = "Requested";
            _context.RequestExports.Update(requestExport);

            await _context.SaveChangesAsync();

            return warehouseRequests.FirstOrDefault();
        }

        public async Task<WarehouseRequestExport> GetByIdAsync(int id)
        {
            return await _context.WarehouseRequestExports
                .Include(x => x.Product)
                .Include(x => x.RequestExport)
                .FirstOrDefaultAsync(x => x.WarehouseRequestExportId == id);
        }

        public async Task UpdateAsync(WarehouseRequestExport request)
        {
            _context.WarehouseRequestExports.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WarehouseRequestExport>> GetByWarehouseAndRequestExportAsync(int warehouseId, int requestExportId)
        {
            return await _context.WarehouseRequestExports
                .Where(x => x.WarehouseId == warehouseId && x.RequestExportId == requestExportId)
                .ToListAsync();
        }

        public async Task UpdateManyAsync(List<WarehouseRequestExport> requests)
        {
            _context.WarehouseRequestExports.UpdateRange(requests);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WarehouseRequestExport>> GetByWarehouseIdAsync(long warehouseId)
        {
            return await _context.WarehouseRequestExports
                .Include(x => x.Product)
                .Include(x => x.RequestExport)
                    .ThenInclude(re => re.Order)
                        .ThenInclude(o => o.RequestProduct)
                            .ThenInclude(rp => rp.AgencyAccount)
                .ToListAsync();
        }

        public async Task<List<WarehouseRequestExport>> GetByRequestExportIdAsync(int requestExportId)
        {
            return await _context.WarehouseRequestExports
                .Where(x => x.RequestExportId == requestExportId)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<WarehouseRequestExport> entities)
        {
            await _context.WarehouseRequestExports.AddRangeAsync(entities);
            // Không SaveChanges ở đây, vì ta muốn gọi SaveChanges ở service/tầng trên
        }

    }
}
