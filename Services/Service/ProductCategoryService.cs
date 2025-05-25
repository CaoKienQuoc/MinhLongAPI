using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.DTO.Product;
using BusinessObject.Models;
using Repo.IRepository;
using Services.IService;

namespace Services.Service
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IProductCategoryRepository _repository;

        public ProductCategoryService(IProductCategoryRepository repository)
        {
            _repository = repository;
        }

        /*public async Task<IEnumerable<ProductCategoryResponseDto>> GetAllCategoriesAsync()
        {
            var categories = await _repository.GetAllAsync();
            return categories.Select(c => new ProductCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedBy = c.CreatedBy,
                CreatedDate = c.CreatedDate,
                UpdatedBy = c.UpdatedBy,
                UpdatedDate = c.UpdatedDate
            }).ToList();
        }*/

        public async Task<IEnumerable<ProductCategoryResponseDto>> GetAllCategoriesAsync()
        {
            var categories = await _repository.GetAllAsync();

            return categories.Select(c => new ProductCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedBy = c.CreatedBy,
                CreatedByName = c.Creator?.Employee?.FullName ?? c.Creator?.Username ?? "Unknown",
                CreatedDate = c.CreatedDate,
                UpdatedBy = c.UpdatedBy,
                UpdatedByName = c.Updater?.Employee?.FullName ?? c.Updater?.Username ?? "Chưa cập nhật",
                UpdatedDate = c.UpdatedDate
            }).ToList();
        }


        /*public async Task<ProductCategoryResponseDto> GetCategoryByIdAsync(long id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return null;

            return new ProductCategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ParentCategoryId = category.ParentCategoryId,
                SortOrder = category.SortOrder,
                Notes = category.Notes,
                IsActive = category.IsActive,
                CreatedBy = category.CreatedBy,
                CreatedDate = category.CreatedDate,
                UpdatedBy = category.UpdatedBy,
                UpdatedDate = category.UpdatedDate
            };
        }*/

        public async Task<ProductCategoryResponseDto> GetCategoryByIdAsync(long id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return null;

            return new ProductCategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Notes = category.Notes,
                IsActive = category.IsActive,
                CreatedBy = category.CreatedBy,
                CreatedByName = category.Creator?.Employee?.FullName ?? category.Creator?.Username ?? "Unknown",
                CreatedDate = category.CreatedDate,
                UpdatedBy = category.UpdatedBy,
                UpdatedByName = category.Updater?.Employee?.FullName ?? category.Updater?.Username ?? "Chưa cập nhật",
                UpdatedDate = category.UpdatedDate
            };
        }


        public async Task<ProductCategoryResponseDto> CreateCategoryAsync(ProductCategoryDto categoryDto, Guid userId)
        {
            var category = new ProductCategory
            {
                CategoryName = categoryDto.CategoryName,
                Notes = categoryDto.Notes,
                IsActive = categoryDto.IsActive,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            var createdCategory = await _repository.AddAsync(category);

            return new ProductCategoryResponseDto
            {
                CategoryId = createdCategory.CategoryId,
                CategoryName = createdCategory.CategoryName,
                Notes = createdCategory.Notes,
                IsActive = createdCategory.IsActive,
                CreatedBy = createdCategory.CreatedBy,
                CreatedDate = createdCategory.CreatedDate
            };
        }

        public async Task<ProductCategoryResponseDto> UpdateCategoryAsync(long id, ProductCategoryDto categoryDto, Guid userId)
        {
            var existingCategory = await _repository.GetByIdAsync(id);
            if (existingCategory == null) return null;

            existingCategory.CategoryName = categoryDto.CategoryName;
            existingCategory.Notes = categoryDto.Notes;
            existingCategory.IsActive = categoryDto.IsActive;
            existingCategory.UpdatedBy = userId;
            existingCategory.UpdatedDate = DateTime.Now;

            var updatedCategory = await _repository.UpdateAsync(existingCategory);

            return new ProductCategoryResponseDto
            {
                CategoryId = updatedCategory.CategoryId,
                CategoryName = updatedCategory.CategoryName,
                Notes = updatedCategory.Notes,
                IsActive = updatedCategory.IsActive,
                CreatedBy = updatedCategory.CreatedBy,
                CreatedDate = updatedCategory.CreatedDate,
                UpdatedBy = updatedCategory.UpdatedBy,
                UpdatedDate = updatedCategory.UpdatedDate
            };
        }

        public async Task<bool> DeleteCategoryAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
