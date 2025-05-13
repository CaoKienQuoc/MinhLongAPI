using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.Models;

namespace Services.IService
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetProductsAsync();
        Task<ProductResponseDto> GetProductByIdAsync(long id);
        Task<ProductResponseDto> CreateProductAsync(ProductDto productDto, Guid userId);
        Task<ProductResponseDto> UpdateProductAsync(long id, UpdateProductDTO productDto, Guid userId);
        Task<bool> DeleteProductAsync(long id);
        Task<List<ProductSimpleResponseDto>> GetProductsByCategoryIdAsync(long categoryId);

        Task<int> GetAvailableStockAsync(long productId);

        Task<List<ProductResponseDto>> GetProductsFromExpiredBatchesAsync();

        Task<List<ProductResponseDto>> GetProductsFromExpiredBatchesByCategoryAsync(long categoryId);
        Task<ProductResponseDto?> GetProductDetailWithExpiredLogicAsync(long productId);
    }
}
