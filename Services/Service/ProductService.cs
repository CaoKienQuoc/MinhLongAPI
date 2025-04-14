using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;

namespace Services.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IImageService _imageService;
        private readonly IWarehouseProductRepository _warehouseProductRepository;
        private readonly ITemporaryWarehouseExportRepository _temporaryRepository;

        public ProductService(IProductRepository repository, IImageService imageService, IWarehouseProductRepository warehouseProductRepository, ITemporaryWarehouseExportRepository temporaryWarehouseExportRepository)
        {
            _repository = repository;
            _imageService = imageService;
            _warehouseProductRepository = warehouseProductRepository;
            _temporaryRepository = temporaryWarehouseExportRepository;
        }

        /*public async Task<List<ProductResponseDto>> GetProductsAsync()
        {
            var products = await _repository.GetProductsAsync();

            *//*foreach (var product in products)
            {
                var totalAvailable = await _warehouseProductRepository.GetTotalAvailableStockByProductIdAsync(product.ProductId);

                if (product.AvailableStock != totalAvailable)
                {
                    product.AvailableStock = totalAvailable;
                    await _repository.UpdateAsync(product);
                }
            }

            // ✅ Lưu 1 lần duy nhất (tối ưu performance)
            await _repository.SaveChangesAsync();*//*

            return products.Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit,
                DefaultExpiration = p.DefaultExpiration,
                CategoryId = p.CategoryId,
                Description = p.Description,
                TaxId = p.TaxId,
                CreatedBy = p.CreatedBy,
                CreatedByName = p.Creator?.Employee?.FullName ?? p.Creator?.Username ?? "Unknown",
                CreatedDate = p.CreatedDate,
                UpdatedBy = p.UpdatedBy,
                UpdatedByName = p.Updater?.Employee?.FullName ?? p.Updater?.Username ?? "Chưa cập nhật",
                UpdatedDate = p.UpdatedDate,
                AvailableStock = p.AvailableStock,
                Price = p.Price,
                Images = p.Images.Select(img => img.ImageUrl).ToList()
            }).ToList();
        }

        public async Task<ProductResponseDto> GetProductByIdAsync(long id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            *//*// ✅ Tính tồn kho theo tất cả kho
            var totalAvailable = await _warehouseProductRepository.GetTotalAvailableStockByProductIdAsync(product.ProductId);

            // ✅ Cập nhật vào Product.AvailableStock nếu khác giá trị hiện tại
            if (product.AvailableStock != totalAvailable)
            {
                product.AvailableStock = totalAvailable;
                await _repository.UpdateAsync(product);            // cập nhật entity
                await _repository.SaveChangesAsync();              // lưu thay đổi vào DB
            }*//*

            return new ProductResponseDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Unit = product.Unit,
                DefaultExpiration = product.DefaultExpiration,
                CategoryId = product.CategoryId,
                Description = product.Description,
                TaxId = product.TaxId,
                CreatedBy = product.CreatedBy,
                CreatedByName = product.Creator?.Employee?.FullName ?? product.Creator?.Username ?? "Unknown",
                CreatedDate = product.CreatedDate,
                UpdatedBy = product.UpdatedBy,
                UpdatedByName = product.Updater?.Employee?.FullName ?? product.Updater?.Username ?? "Chưa cập nhật",
                UpdatedDate = product.UpdatedDate,
                AvailableStock = product.AvailableStock, // ✅ tồn kho thực tế
                Price = product.Price,
                // ✅ Lấy danh sách URL hình ảnh từ database
                Images = product.Images.Select(img => img.ImageUrl).ToList()
            };
        }*/

        public async Task<ProductResponseDto> GetProductByIdAsync(long id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            var availableStock = await GetAvailableStockAsync(product.ProductId);

            return new ProductResponseDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Unit = product.Unit,
                DefaultExpiration = product.DefaultExpiration,
                CategoryId = product.CategoryId,
                Description = product.Description,
                TaxId = product.TaxId,
                CreatedBy = product.CreatedBy,
                CreatedByName = product.Creator?.Employee?.FullName ?? product.Creator?.Username ?? "Unknown",
                CreatedDate = product.CreatedDate,
                UpdatedBy = product.UpdatedBy,
                UpdatedByName = product.Updater?.Employee?.FullName ?? product.Updater?.Username ?? "Chưa cập nhật",
                UpdatedDate = product.UpdatedDate,
                AvailableStock = availableStock,
                Price = product.Price,
                Images = product.Images.Select(img => img.ImageUrl).ToList()
            };
        }

        public async Task<List<ProductResponseDto>> GetProductsAsync()
        {
            var products = await _repository.GetProductsAsync();
            var result = new List<ProductResponseDto>();

            foreach (var product in products)
            {
                var availableStock = await GetAvailableStockAsync(product.ProductId);

                result.Add(new ProductResponseDto
                {
                    ProductId = product.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    DefaultExpiration = product.DefaultExpiration,
                    CategoryId = product.CategoryId,
                    Description = product.Description,
                    TaxId = product.TaxId,
                    CreatedBy = product.CreatedBy,
                    CreatedByName = product.Creator?.Employee?.FullName ?? product.Creator?.Username ?? "Unknown",
                    CreatedDate = product.CreatedDate,
                    UpdatedBy = product.UpdatedBy,
                    UpdatedByName = product.Updater?.Employee?.FullName ?? product.Updater?.Username ?? "Chưa cập nhật",
                    UpdatedDate = product.UpdatedDate,
                    AvailableStock = availableStock,
                    Price = product.Price,
                    Images = product.Images.Select(img => img.ImageUrl).ToList()
                });
            }

            return result;
        }


        public async Task<ProductResponseDto> CreateProductAsync(ProductDto model, Guid userId)
        {
            var product = new Product
            {
                ProductCode = model.ProductCode,
                ProductName = model.ProductName,
                Unit = model.Unit,
                DefaultExpiration = model.DefaultExpiration,
                CategoryId = model.CategoryId,
                Description = model.Description,
                TaxId = model.TaxId,
                CreatedBy = userId,
                CreatedDate = DateTime.Now            };

            // ✅ Tạo product trước
            var createdProduct = await _repository.AddAsync(product); // KHÔNG truyền imageUrls

            // ✅ Nếu có ảnh, upload ảnh và lưu vào bảng Image
            if (model.Images != null && model.Images.Count > 0)
            {
                var imageModel = new ImageModel
                {
                    Files = model.Images
                };

                await _imageService.UploadImagesAsync(imageModel, createdProduct.ProductId);
            }

            return await GetProductByIdAsync(createdProduct.ProductId);
        }

        public async Task<ProductResponseDto> UpdateProductAsync(long id, UpdateProductDTO productDto, Guid userId)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            // ✅ Cập nhật thông tin sản phẩm
            product.ProductName = productDto.ProductName ?? product.ProductName;
            product.Unit = productDto.Unit ?? product.Unit;
            product.DefaultExpiration = productDto.DefaultExpiration ?? product.DefaultExpiration;
            product.CategoryId = productDto.CategoryId;
            product.Description = productDto.Description ?? product.Description;
            product.TaxId = productDto.TaxId ?? product.TaxId;
            product.UpdatedBy = userId;
            product.UpdatedDate = DateTime.Now;

            // ✅ Cập nhật sản phẩm trước
            var updatedProduct = await _repository.UpdateAsync(product);

            // ✅ Nếu có ảnh mới, thay thế ảnh cũ bằng ảnh mới trong bảng Images
            if (productDto.Images != null && productDto.Images.Count > 0)
            {
                // Xóa ảnh cũ trong bảng Images
                await _imageService.DeleteImagesByProductIdAsync(updatedProduct.ProductId);

                // Tạo model ảnh để upload
                var imageModel = new ImageModel
                {
                    Files = productDto.Images // List<IFormFile>
                };

                // Upload ảnh mới và lưu vào bảng Images
                await _imageService.UploadImagesAsync(imageModel, updatedProduct.ProductId);
            }

            return await GetProductByIdAsync(updatedProduct.ProductId);
        }
        public async Task<int> GetAvailableStockAsync(long productId)
        {
            var total = await _warehouseProductRepository.GetTotalAvailableStockByProductIdAsync(productId);
            var reserved = await _temporaryRepository.GetReservedStockByProductIdAsync(productId);

            return (int)Math.Max(total, 0);
        }


        public async Task<bool> DeleteProductAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }


        public async Task<List<ProductSimpleResponseDto>> GetProductsByCategoryIdAsync(long categoryId)
        {
            var products = await _repository.GetProductsByCategoryIdAsync(categoryId);
            return products.Select(p => new ProductSimpleResponseDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Images = p.Images.Select(img => img.ImageUrl).ToList()
            }).ToList();
        }

    }
}
