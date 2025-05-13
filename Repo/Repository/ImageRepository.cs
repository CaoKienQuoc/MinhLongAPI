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
        public async Task<ReturnRequestImage> AddReturnImageAsync(ReturnRequestImage image)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (image == null)
                    throw new ArgumentNullException(nameof(image), "Image không được null.");

                if (string.IsNullOrWhiteSpace(image.ImageUrl))
                    throw new ArgumentException("ImageUrl không được rỗng.", nameof(image.ImageUrl));

                if (string.IsNullOrWhiteSpace(image.PublicId))
                    throw new ArgumentException("PublicId không được rỗng.", nameof(image.PublicId));

                /*if (image.ReturnRequestImageId <= 0)
                    throw new ArgumentException("ReturnRequestDetailId không hợp lệ.", nameof(image.ReturnRequestDetailId));*/

                // Kiểm tra ReturnRequestDetail có tồn tại không
                var returnRequestDetail = await _context.ReturnRequestDetails.FindAsync(image.ReturnRequestDetailId);
                if (returnRequestDetail == null)
                    throw new Exception($"Không tìm thấy ReturnRequestDetail với ID {image.ReturnRequestDetailId}.");

                // Thêm ảnh
                _context.ReturnRequestImages.Add(image);
                await _context.SaveChangesAsync();
                return image;
            }
            catch (DbUpdateException dbEx)
            {
                // Lỗi từ database (khóa ngoại, trùng lặp, constraint)
                Console.WriteLine($"Lỗi lưu ảnh vào database: {dbEx.InnerException?.Message}");
                throw new Exception("Lỗi lưu ảnh vào database. Vui lòng thử lại sau.");
            }
            catch (Exception ex)
            {
                // Lỗi chung
                Console.WriteLine($"Lỗi khi thêm ảnh trả hàng: {ex.Message}");
                throw new Exception("Đã xảy ra lỗi khi thêm ảnh trả hàng. Vui lòng kiểm tra lại dữ liệu.");
            }
        }


        public async Task<ReturnRequestImage> UpdateReturnImageAsync(ReturnRequestImage image)
        {
            _context.ReturnRequestImages.Update(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<List<ReturnRequestImage>> AddRangeAsync(List<ReturnRequestImage> images)
        {
            _context.ReturnRequestImages.AddRange(images);
            await _context.SaveChangesAsync();
            return images;
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

        public async Task SaveChatImageAsync(ChatMessageImage image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            _context.ChatMessageImages.Add(image);
            await _context.SaveChangesAsync();
        }
    }
}
