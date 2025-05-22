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
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly MinhLongDbContext _context;

        public RefreshTokenRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked);
        }

        public async Task RevokeTokenAsync(string token)
        {
            var tokenEntity = await GetByTokenAsync(token);
            if (tokenEntity != null)
            {
                tokenEntity.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
