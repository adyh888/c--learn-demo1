using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        [HttpGet]
        public IActionResult Search([FromQuery] string keyword)
        {
            if(string.IsNullOrEmpty(keyword))
            {
                return BadRequest(new { error = "Keyword is required" });
            };
            return Ok(new { keyword = keyword, results = $"Found results for:{keyword}" });
        }
        
        [HttpGet("advanced")]
        public IActionResult AdvancedSearch([FromQuery] string keyword,[FromQuery] decimal? minPrice,[FromQuery] decimal? maxPrice,[FromQuery]string? sort="name")
        {
            var query = new
            {
                keyword = keyword,
                minPrice = minPrice ?? 0,
                maxPrice = maxPrice ?? 999999,
                sort = sort
            };

            return Ok(new
            {
                query = query,
                message =
                    $"Advanced search results for:{keyword} with price between {minPrice ?? 0} and {maxPrice ?? 999999} sorted by {sort}"
            });
        }

        [HttpGet("filter")]
        public IActionResult FilterProducts([FromQuery] ProductFilter filter)
        {
            return Ok(new
            {
                filter = filter,
                message = "Products filtered"
            });
        }
        
        //查询参数模型
        public class ProductFilter
        {
            public string? Keyword { get; set; }
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public string? Category { get; set; }

            public bool InStock { get; set; } = true;
        };
    }
}
