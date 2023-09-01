using FurnitureStore.Data;
using FurnitureStore.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly FurnitureStoreContext _context;
        public ProductCategoriesController(FurnitureStoreContext context)
        {
            _context = context;
        }
        // GET: api/<ProductCategoriesController>
        [HttpGet]
        public async Task<IEnumerable<ProductCategory>> Get()
        {
            var productCategories = await _context.ProductCategories.ToListAsync();
            return productCategories;
        }

        // GET api/<ProductCategoriesController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GeDetailst(int id)
        {
            var productCategory = await _context.ProductCategories.SingleOrDefaultAsync(x => x.Id == id);
            if (productCategory == null) return NotFound();
            return Ok(productCategory);
        }

        // POST api/<ProductCategoriesController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductCategory productCategory)
        {
            _context.ProductCategories.Add(productCategory);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/<ProductCategoriesController>/5
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ProductCategory productCategory)
        {
            _context.ProductCategories.Update(productCategory);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/<ProductCategoriesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var productCategory = await _context.ProductCategories.SingleOrDefaultAsync(x => x.Id == id);
            if (productCategory == null) return NotFound();
            _context.ProductCategories.Remove(productCategory);
            return NoContent();
        }
    }
}
