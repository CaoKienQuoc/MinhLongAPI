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
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace Services.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IImageService _imageService;
        private readonly IWarehouseProductRepository _warehouseProductRepository;
        private readonly ITemporaryWarehouseExportRepository _temporaryRepository;
        private readonly IBatchRepository _batchRepository;


        public ProductService(IProductRepository repository, IImageService imageService, IWarehouseProductRepository warehouseProductRepository,
            ITemporaryWarehouseExportRepository temporaryWarehouseExportRepository, IBatchRepository batchRepository)
        {
            _repository = repository;
            _imageService = imageService;
            _warehouseProductRepository = warehouseProductRepository;
            _temporaryRepository = temporaryWarehouseExportRepository;
            _batchRepository = batchRepository;
        }

        
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
            var random = new Random();
            var product = new Product
            {
                ProductCode = $"SP{DateTime.Now.Ticks}-{random.Next(1000, 9999)}",
                ProductName = model.ProductName,
                Unit = model.Unit,
                DefaultExpiration = model.DefaultExpiration,
                CategoryId = model.CategoryId,
                Description = model.Description,
                TaxId = model.TaxId,
                CreatedBy = userId,
                CreatedDate = DateTime.Now            };
            //abcd
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

            // ✅ Cập nhật lẻ từng trường nếu được truyền vào
            if (!string.IsNullOrEmpty(productDto.ProductName))
                product.ProductName = productDto.ProductName;

            if (!string.IsNullOrEmpty(productDto.Unit))
                product.Unit = productDto.Unit;

            if (productDto.DefaultExpiration.HasValue)
                product.DefaultExpiration = productDto.DefaultExpiration.Value;

            if (productDto.CategoryId.HasValue)
                product.CategoryId = productDto.CategoryId.Value;

            if (!string.IsNullOrEmpty(productDto.Description))
                product.Description = productDto.Description;

            if (productDto.TaxId.HasValue)
                product.TaxId = productDto.TaxId.Value;

            product.UpdatedBy = userId;
            product.UpdatedDate = DateTime.Now;

            // ✅ Cập nhật sản phẩm trong DB
            var updatedProduct = await _repository.UpdateAsync(product);

            // ✅ Nếu có ảnh mới, thay thế ảnh cũ
            if (productDto.Images != null && productDto.Images.Count > 0)
            {
                await _imageService.DeleteImagesByProductIdAsync(updatedProduct.ProductId);

                var imageModel = new ImageModel
                {
                    Files = productDto.Images
                };

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

        public async Task<List<ProductResponseDto>> GetProductsFromExpiredBatchesAsync()
        {
            var batches = await _batchRepository.GetExpiredSoonBatchesAsync();

            var grouped = batches.GroupBy(b => b.ProductId);
            var now = DateTime.Now;
            var result = new List<ProductResponseDto>();

            foreach (var group in grouped)
            {
                var product = group.First().Product;
                var totalQty = group.Sum(b => b.Quantity);
                var minExpiry = group.Min(b => b.ExpiryDate);
                var monthsLeft = ((minExpiry.Year - now.Year) * 12) + minExpiry.Month - now.Month;

                decimal? adjustedPrice = product.Price;
                if (monthsLeft < 2) adjustedPrice *= 0.7m;
                else if (monthsLeft < 4) adjustedPrice *= 0.8m;
                else if (monthsLeft < 6) adjustedPrice *= 0.9m;

                result.Add(new ProductResponseDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductCode = product.ProductCode,
                    Unit = product.Unit,
                    Description = product.Description,
                    CategoryId = product.CategoryId,
                    Price = adjustedPrice,
                    AvailableStock = totalQty,
                    Images = product.Images.Select(i => i.ImageUrl).ToList()
                });
            }

            return result.OrderByDescending(p => p.AvailableStock > 0).ToList();
        }

        public async Task<List<ProductResponseDto>> GetProductsFromExpiredBatchesByCategoryAsync(long categoryId)
        {
            var batches = await _batchRepository.GetExpiredSoonBatchesByCategoryAsync(categoryId);
            var now = DateTime.Now;

            // Group các batch theo Product
            var grouped = batches
                .GroupBy(b => b.ProductId)
                .ToList();

            var result = new List<ProductResponseDto>();

            foreach (var group in grouped)
            {
                var product = group.First().Product;
                var totalQty = group.Sum(b => b.Quantity);
                var minExpiry = group.Min(b => b.ExpiryDate);

                var monthsLeft = ((minExpiry.Year - now.Year) * 12) + minExpiry.Month - now.Month;

                decimal? adjustedPrice = product.Price;

                if (monthsLeft < 2)
                {
                    adjustedPrice *= 0.7m;
                }
                else if (monthsLeft < 4)
                {
                    adjustedPrice *= 0.8m;
                }
                else if (monthsLeft < 6)
                {
                    adjustedPrice *= 0.9m;
                }

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
                    CreatedDate = product.CreatedDate,
                    UpdatedBy = product.UpdatedBy,
                    UpdatedDate = product.UpdatedDate,
                    AvailableStock = totalQty,
                    Price = adjustedPrice,
                    Images = product.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
                });
            }

            return result
                .OrderByDescending(p => p.AvailableStock > 0)
                .ToList();
        }

        public async Task<ProductResponseDto?> GetProductDetailWithExpiredLogicAsync(long productId)
        {
            var batches = await _batchRepository.GetExpiredSoonBatchesByProductIdAsync(productId);
            var product = batches.FirstOrDefault()?.Product;

            if (product == null) return null;

            var totalQty = batches.Sum(b => b.Quantity);
            var now = DateTime.Now;
            var minExpiry = batches.Min(b => b.ExpiryDate);
            var monthsLeft = ((minExpiry.Year - now.Year) * 12) + minExpiry.Month - now.Month;

            decimal? adjustedPrice = product.Price;
            if (monthsLeft < 2) adjustedPrice *= 0.7m;
            else if (monthsLeft < 4) adjustedPrice *= 0.8m;
            else if (monthsLeft < 6) adjustedPrice *= 0.9m;

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
                CreatedDate = product.CreatedDate,
                UpdatedBy = product.UpdatedBy,
                UpdatedDate = product.UpdatedDate,
                AvailableStock = totalQty,
                Price = adjustedPrice,
                Images = product.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
            };
        }

    }
}
