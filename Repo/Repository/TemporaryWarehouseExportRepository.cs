using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using DataAccessLayer;
using Repo.IRepository;

namespace Repo.Repository
{
    public class TemporaryWarehouseExportRepository : ITemporaryWarehouseExportRepository
    {
        private readonly MinhLongDbContext _context;

        public TemporaryWarehouseExportRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TemporaryStockExport entity)
        {
            await _context.TemporaryStockExports.AddAsync(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
