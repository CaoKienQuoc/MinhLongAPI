using BusinessObject.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Repo.IRepository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    using BusinessObject.DTO.Product;
    using CloudinaryDotNet;
    using CloudinaryDotNet.Actions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Repo.Repository;
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public class ImageService : IImageService
    {
        private readonly IImageRepository _repo;
        private readonly IOrderRepository _orderRepository;
        private readonly Cloudinary _cloudinary;
        private readonly long _maxFileSize; // Kích thước tối đa (byte)
        private readonly int _maxWidth; // Chiều rộng tối đa ảnh
        private readonly int _maxHeight; // Chiều cao tối đa ảnh
        private readonly int _quality; // Chất lượng nén

        public ImageService(IImageRepository repo, IConfiguration config, IOrderRepository orderRepository)
        {
            _repo = repo;

            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);

            // Đọc cấu hình từ appsettings.json
            _maxFileSize = config.GetValue<long>("Cloudinary:MaxFileSize", 5 * 1024 * 1024); // Mặc định 5MB
            _maxWidth = config.GetValue<int>("Cloudinary:MaxWidth", 1024); // Mặc định 1024px
            _maxHeight = config.GetValue<int>("Cloudinary:MaxHeight", 1024); // Mặc định 1024px
            _quality = config.GetValue<int>("Cloudinary:Quality", 80); // Chất lượng ảnh nén (mặc định 80%)
            _orderRepository = orderRepository;
        }

        public async Task<List<Image>> UploadImagesAsync(ImageModel imageModel, long productId)
        {
            if (imageModel.Files == null || imageModel.Files.Count == 0)
                throw new Exception("No files uploaded.");

            var uploadedImages = new List<Image>();

            foreach (var file in imageModel.Files)
            {
                // Kiểm tra kích thước file trước khi xử lý
                if (file.Length > _maxFileSize)
                {
                    Console.WriteLine($"File {file.FileName} quá lớn ({file.Length / (1024 * 1024)}MB), đang xử lý giảm dung lượng...");

                    // Giảm dung lượng ảnh
                    using var resizedStream = ResizeAndCompressImage(file, _maxWidth, _maxHeight, _quality);

                    if (resizedStream.Length > _maxFileSize)
                        throw new Exception($"Sau khi nén, file {file.FileName} vẫn quá lớn ({resizedStream.Length / (1024 * 1024)}MB).");

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, resizedStream),
                        PublicId = Guid.NewGuid().ToString(),
                        Overwrite = true
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Upload thất bại: {uploadResult.Error?.Message}");

                    var image = new Image
                    {
                        ImageUrl = uploadResult.SecureUrl.ToString(),
                        PublicId = uploadResult.PublicId,
                        ProductId = productId
                    };

                    uploadedImages.Add(await _repo.AddAsync(image));
                }
                else
                {
                    // Nếu file nhỏ hơn giới hạn, upload trực tiếp
                    using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        PublicId = Guid.NewGuid().ToString(),
                        Overwrite = true
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Upload thất bại: {uploadResult.Error?.Message}");

                    var image = new Image
                    {
                        ImageUrl = uploadResult.SecureUrl.ToString(),
                        PublicId = uploadResult.PublicId,
                        ProductId = productId
                    };

                    uploadedImages.Add(await _repo.AddAsync(image));
                }
            }

            return uploadedImages;
        }

        private Stream ResizeAndCompressImage(IFormFile file, int maxWidth, int maxHeight, int quality)
        {
            using var inputStream = file.OpenReadStream();
            using var originalImage = SKBitmap.Decode(inputStream);

            int newWidth = originalImage.Width;
            int newHeight = originalImage.Height;

            // Tính toán tỷ lệ resize
            if (originalImage.Width > maxWidth || originalImage.Height > maxHeight)
            {
                float aspectRatio = (float)originalImage.Width / originalImage.Height;
                if (aspectRatio > 1)
                {
                    newWidth = maxWidth;
                    newHeight = (int)(maxWidth / aspectRatio);
                }
                else
                {
                    newHeight = maxHeight;
                    newWidth = (int)(maxHeight * aspectRatio);
                }
            }

            using var resizedImage = originalImage.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
            using var image = SKImage.FromBitmap(resizedImage);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            var outputStream = new MemoryStream();
            data.SaveTo(outputStream);
            outputStream.Position = 0; // Reset stream về đầu để upload
            return outputStream;
        }
    

    public async Task<List<Image>> UpdateImagesByProductIdAsync(long productId, ImageModel imageModel)
        {
            // ✅ Lấy tất cả ảnh hiện có theo ProductId
            var existingImages = await _repo.GetImagesByProductIdAsync(productId);
            if (existingImages == null || existingImages.Count == 0)
            {
                throw new Exception($"Không tìm thấy hình ảnh nào cho ProductId = {productId}");
            }

            if (imageModel.Files == null || imageModel.Files.Count != existingImages.Count)
            {
                throw new Exception("Số lượng ảnh mới không khớp với ảnh hiện tại. Vui lòng upload đúng số lượng.");
            }

            var updatedImages = new List<Image>();

            for (int i = 0; i < existingImages.Count; i++)
            {
                var oldImage = existingImages[i];
                var newFile = imageModel.Files[i];

                // ✅ Xóa ảnh cũ trên Cloudinary
                if (!string.IsNullOrEmpty(oldImage.PublicId))
                {
                    await _cloudinary.DestroyAsync(new DeletionParams(oldImage.PublicId));
                }

                // ✅ Upload ảnh mới lên Cloudinary
                using var stream = newFile.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(newFile.FileName, stream),
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Upload thất bại cho ảnh {newFile.FileName}: {uploadResult.Error?.Message}");
                }

                // ✅ Cập nhật URL và PublicId của bản ghi cũ
                oldImage.ImageUrl = uploadResult.SecureUrl.ToString();
                oldImage.PublicId = uploadResult.PublicId;

                // ✅ Update lại bản ghi trong DB
                updatedImages.Add(await _repo.UpdateImageAsync(oldImage));
            }

            return updatedImages;
        }

        public async Task DeleteImagesByProductIdAsync(long productId)
        {
            var images = await _repo.GetImagesByProductIdAsync(productId);
            if (images.Any())
            {
                await _repo.DeleteRangeAsync(images);
            }
        }

        public async Task<List<ReturnRequestImage>> UploadReturnRequestImagesAsync(Guid orderDetailId, Guid returnRequestDetailId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new Exception("Không có ảnh được tải lên.");

            var orderDetail = await _orderRepository.GetOrderDetailByIdAsync(orderDetailId);
            var productId = orderDetail.ProductId;


            var result = new List<ReturnRequestImage>();

            foreach (var file in files)
            {
                Stream stream;

                if (file.Length > _maxFileSize)
                {
                    stream = ResizeAndCompressImage(file, _maxWidth, _maxHeight, _quality);
                }
                else
                {
                    stream = file.OpenReadStream();
                }

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"Upload thất bại: {uploadResult.Error?.Message}");

                result.Add(new ReturnRequestImage
                {
                    ReturnRequestDetailId = returnRequestDetailId,
                    ImageUrl = uploadResult.SecureUrl.ToString(),
                    UploadedAt = DateTime.UtcNow
                });
            }

            return await _repo.AddRangeAsync(result);
        }
    }
}
