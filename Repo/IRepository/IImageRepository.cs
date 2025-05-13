using BusinessObject.DTO;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repo.IRepository
{
    public interface IImageRepository
    {
        Task<Image> AddAsync(Image image);
        Task<Product> GetByIdAsync(long productId);  // ✅ Trả về 1 Product duy nhất
        Task<List<Image>> GetImagesByProductIdAsync(long productId); // ✅ Trả về danh sách ảnh
        Task<Image> UpdateImageAsync(Image image);
        Task DeleteRangeAsync(List<Image> images);
        Task SaveChangesAsync();
        Task<ReturnRequestImage> UpdateReturnImageAsync(ReturnRequestImage image);
        Task<ReturnRequestImage> AddReturnImageAsync(ReturnRequestImage image);
        Task<List<ReturnRequestImage>> AddRangeAsync(List<ReturnRequestImage> images);

        Task SaveChatImageAsync(ChatMessageImage image);

    }
}
