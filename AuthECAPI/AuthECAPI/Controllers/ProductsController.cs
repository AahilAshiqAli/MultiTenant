using AuthECAPI.Services.DTOs;
using AuthECAPI.Services.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;


        public ProductsController(IProductService productService)
        {
            _productService = productService; // inject the products service
        }

        // Get list of products
        [HttpGet]
        public IActionResult Get()
        {
            var list = _productService.GetAllProducts();
            return Ok(list);
        }

        // Create a new product
        [HttpPost]
        public async Task<IActionResult> PostAsync(CreateProductRequest request)
        {
            try
            {
                
                if (request.File == null || request.File.Length == 0)
                    return BadRequest("No file uploaded.");

                var allowedTypes = new[] { "audio", "video" };
                if (!allowedTypes.Any(t => request.File.ContentType.StartsWith(t)))
                    return BadRequest("Only audio and video files are allowed.");

                var result = await _productService.CreateProduct(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception: " + ex.Message);
                Console.WriteLine("❌ Stack Trace: " + ex.StackTrace);
                // Optionally log the exception here
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        // Delete a product by id
        [HttpDelete("{id}")]
        public IActionResult Delete(string name)
        {
            var result = _productService.DeleteProduct(name);
            return Ok(result);
        }

    }
}
