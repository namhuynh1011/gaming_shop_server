using gaming_shop_api.Models;
using gaming_shop_server.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gaming_shop_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]

    public class BrandAPIController : ControllerBase
    {
        private readonly IBrandRepository _brandRepo;

        public BrandAPIController(IBrandRepository brandRepo)
        {
            _brandRepo = brandRepo;
        }

        // GET: api/BrandAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
        {
            var brands = await _brandRepo.GetAllAsync();
            return Ok(brands);
        }

        // GET: api/BrandAPI/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrand(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand == null)
                return NotFound();
            return Ok(brand);
        }

        // POST: api/BrandAPI
        [HttpPost]
        public async Task<ActionResult<Brand>> PostBrand(Brand brand)
        {
            await _brandRepo.AddAsync(brand);
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }

        // PUT: api/BrandAPI/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBrand(int id, Brand brand)
        {
            var existing = await _brandRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound();
            await _brandRepo.UpdateAsync(id, brand);
            return NoContent();
        }

        // DELETE: api/BrandAPI/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand == null)
                return NotFound();
            await _brandRepo.DeleteAsync(id);
            return NoContent();
        }
    }
}
