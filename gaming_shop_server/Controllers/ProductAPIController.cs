using gaming_shop_api.Models;
using gaming_shop_server.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace gaming_shop_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProductAPIController : ControllerBase
    {
        private readonly IProductRepository _productRepo;
        public ProductAPIController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _productRepo.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Product>> PostProduct([FromForm] ProductCreateDto dto)
        {
            string? imageUrl = await SaveImage(dto.ImageFile);

            var product = new Product
            {
                ProductName = dto.ProductName,
                Price = dto.Price,
                BrandId = dto.BrandId,
                CategoryId = dto.CategoryId,
                Description = dto.Description,
                ImageUrl = imageUrl
            };

            var created = await _productRepo.AddAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductCreateDto dto)
        {
            var existing = await _productRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            string? imageUrl = existing.ImageUrl;
            if (dto.ImageFile != null)
            {
                imageUrl = await SaveImage(dto.ImageFile);
            }

            var updatedProduct = new Product
            {
                Id = id,
                ProductName = dto.ProductName,
                Price = dto.Price,
                BrandId = dto.BrandId,
                CategoryId = dto.CategoryId,
                Description = dto.Description,
                ImageUrl = imageUrl
            };

            var updated = await _productRepo.UpdateAsync(id, updatedProduct);
            if (updated == null)
                return NotFound();
            return NoContent();
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted = await _productRepo.DeleteAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
        private async Task<string?> SaveImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("No image file provided.");

            var folderPath = Path.Combine("wwwroot/images");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var savePath = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + fileName;
        }
    }
}

public class ProductCreateDto
{
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageFile { get; set; }
}
