using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            return Ok(new { message = "Get all products" });
        }
        
        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            return Ok(new {id = id ,name = $"Product {id}" });
        }


        [HttpGet("featured")]
        public IActionResult GetFaturedProducts()
        {
            return Ok(new { message = "Get featured products" });
        }

        [HttpGet("[action]")]
        public IActionResult Finish()
        {
            return Ok(new { message = "Finish products" });
        }
        [HttpGet("[action]")]
        public IActionResult Search()
        {
            return Ok(new { message = "Search products" });
        }

        [HttpGet("category/{category}/page/{page}")]
        public IActionResult GetByCategory(string category, int page)
        {
            return Ok(new { category = category, page = page });
        }
        
    }
}
