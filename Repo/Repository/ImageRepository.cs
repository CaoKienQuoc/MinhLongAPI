using BusinessObject.DTO;
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
    public class ImageRepository : IImageRepository
    {
        private readonly MinhLongDbContext _context;

        public ImageRepository(MinhLongDbContext context)
        {
            _context = context;
        }

        public async Task<Image> AddAsync(Image image)
        {
            _context.Images.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<Image> UpdateImageAsync(Image image)
        {
            _context.Images.Update(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<Product> GetByIdAsync(long productId)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<List<Image>> GetImagesByProductIdAsync(long productId)
        {
            return await _context.Images.Where(img => img.ProductId == productId).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRangeAsync(List<Image> images)
        {
            _context.Images.RemoveRange(images);
            await _context.SaveChangesAsync();
        }

        public async Task<ReturnRequestImage> AddAsync(ReturnRequestImage image)
        {
            _context.ReturnRequestImages.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<List<ReturnRequestImage>> AddRangeAsync(List<ReturnRequestImage> images)
        {
            _context.ReturnRequestImages.AddRange(images);
            await _context.SaveChangesAsync();
            return images;
        }
    }
}
