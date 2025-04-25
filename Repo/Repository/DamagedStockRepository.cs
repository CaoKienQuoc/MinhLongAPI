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
    public class DamagedStockRepository : IDamagedStockRepository
    {
        private readonly MinhLongDbContext _context;

        public DamagedStockRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<DamagedStock> stocks)
        {
            _context.DamagedStocks.AddRange(stocks);
            await _context.SaveChangesAsync();
        }
    }

}
