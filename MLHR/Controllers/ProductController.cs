using BusinessObject.DTO.Product;
using BusinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;
using Services.Service;
using System.Security.Claims;

namespace MLHR.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly IImageService _imageService;

        public ProductController(IProductService service, IImageService imageService)
        {
            _service = service;
            _imageService = imageService;
        }


        [HttpGet("product")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _service.GetProductsAsync();
            if (products == null)
            {
                return NotFound(new { message = "Không có dữ liệu." });
            }
            return Ok(products);
        }

        [HttpGet("product/{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(long id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        /*[HttpPost("product")]
        [Authorize(Roles = "4")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] ProductDto productDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is invalid." });
            }

            var createdProduct = await _service.CreateProductAsync(productDto, userId);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.ProductId }, createdProduct);
        }*/

        [HttpPost("product")]
        [Authorize(Roles = "4")]
        [Consumes("multipart/form-data")] // ✅ Bắt buộc để hỗ trợ file upload
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] ProductDto model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is invalid." });
            }

            var createdProduct = await _service.CreateProductAsync(model, userId);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.ProductId }, createdProduct);
        }



        [HttpPut("product/{id}")]
        [Authorize(Roles = "4")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(long id, [FromForm] UpdateProductDTO productDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is invalid." });
            }

            var updatedProduct = await _service.UpdateProductAsync(id, productDto, userId);
            if (updatedProduct == null) return NotFound();

            return Ok(updatedProduct);
        }

        [HttpDelete("product/{id}")]
        [Authorize(Roles = "4")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var result = await _service.DeleteProductAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ProductSimpleResponseDto>>> GetProductsByCategory(long categoryId)
        {
            var products = await _service.GetProductsByCategoryIdAsync(categoryId);
            return Ok(products);
        }

        /*[HttpPost("upload/{productId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] ImageModel imageModel, long productId)
        {
            try
            {
                var images = await _imageService.UploadImagesAsync(imageModel, productId);
                return Ok(images.Select(image => new
                {
                    image.ImageId,
                    image.ImageUrl,
                    image.ProductId
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }*/


        [HttpPut("update/{productId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateImages([FromRoute] long productId, [FromForm] ImageModel imageModel)
        {
            try
            {
                var updatedImages = await _imageService.UpdateImagesByProductIdAsync(productId, imageModel);
                return Ok(updatedImages.Select(image => new
                {
                    image.ImageId,
                    image.ImageUrl,
                    image.ProductId
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("product/expired")]
        public async Task<IActionResult> GetProductsFromExpiredBatches()
        {
            var products = await _service.GetProductsFromExpiredBatchesAsync();
            return Ok(products);
        }

        [HttpGet("product/expired/category/{categoryId}")]
        public async Task<IActionResult> GetProductsFromExpiredBatchesByCategory(long categoryId)
        {
            var products = await _service.GetProductsFromExpiredBatchesByCategoryAsync(categoryId);
            return Ok(products);
        }

        [HttpGet("product/expired-detail/{productId}")]
        public async Task<IActionResult> GetProductDetailWithExpiredLogic(long productId)
        {
            var product = await _service.GetProductDetailWithExpiredLogicAsync(productId);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm hoặc không có lô EXPIRED_SOON." });

            return Ok(product);
        }


    }
}
