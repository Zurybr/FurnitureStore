using FurnitureStore.Data;
using FurnitureStore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly FurnitureStoreContext _context;
        public ProductsController(FurnitureStoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDatails(int id)
        {
            var product = await _context.Products.SingleOrDefaultAsync(x=>x.Id==id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpGet("GetByCategory/{productCategoryId}")]
        public async Task<IActionResult> GetByCategory(int productCategoryId)
        {
            var products = await _context.Products
                                            .Where(p => p.ProductCategoryId == productCategoryId)
                                            .ToListAsync();
            if (products == null) return NotFound();
            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut]
        public async Task<IActionResult> Put(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var product = _context.Products.SingleOrDefaultAsync(x=>x.Id==id);
            if (product == null) return NotFound();
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
