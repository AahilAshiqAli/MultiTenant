using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.ContentFolder;
using Serilog;
using System.Threading.Tasks;

namespace MultiTenantAPI.Controllers
{   
    [Route("api/[controller]")]
    [ApiController]
    public class ContentsController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ContentsController(IContentService contentService)
        {
            Log.Information("ContentsController instantiated.");
            _contentService = contentService;
        }

        // Get list of products
        [HttpGet]
        public IActionResult Get([FromQuery] string? name)
        {
            Log.Information("GET request received. Query name: {Name}", name);
            try
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Log.Information("Filtering content by name: {Name}", name);
                    var filtered = _contentService.FilterContentByName(name);
                    Log.Information("Filtered content count: {Count}", filtered?.Count() ?? 0);
                    return Ok(filtered);
                }

                Log.Information("Retrieving all content.");
                var list = _contentService.GetAllContent();
                Log.Information("Total content count: {Count}", list?.Count() ?? 0);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred in Get method.");
                Console.WriteLine("❌ Exception: " + ex.Message);
                Console.WriteLine("❌ Stack Trace: " + ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }


        [HttpGet("stream/{id}")]
        public async Task<IActionResult> Stream(int id)
        {
            Log.Information("Stream request received for id: {Id}", id);
            try
            {
                var result = await _contentService.StreamVideoAsync(id);
                Log.Information("StreamVideoAsync completed for id: {Id}", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                Log.Warning(ex, "KeyNotFoundException in Stream for id: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                Log.Warning(ex, "FileNotFoundException in Stream for id: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (FormatException ex)
            {
                Log.Warning(ex, "FormatException (Invalid range format) in Stream for id: {Id}", id);
                return BadRequest(new { message = "Invalid range format." });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected exception in Stream for id: {Id}", id);
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("upload-url")]
        public IActionResult GetUploadUrl([FromQuery] string fileName)
        {
            try
            {
                var result = _contentService.GenerateUploadUrlAsync(fileName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid argument in upload URL request.");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex, "Unauthorized access when generating upload URL.");
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error occurred while generating upload URL.");
                return StatusCode(500, new { error = "Internal server error while generating upload URL." });
            }
        }

        // Create a new product
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] CreateProductRequest request)
        {
            Log.Information("POST request received. File: {FileName}, ContentType: {ContentType}, Length: {Size}",
                request.OriginalFileName,request.ContentType, request.Size);
            try
            {

                var allowedTypes = new[] { "audio", "video" };
                if (!allowedTypes.Any(t => request.ContentType.StartsWith(t)))
                {
                    Log.Warning("Invalid file type uploaded: {ContentType}", request.ContentType);
                    return BadRequest("Only audio and video files are allowed.");
                }

                Log.Information("Creating content for file: {FileName}", request.OriginalFileName);
                await _contentService.CreateContent(request);
                Log.Information("Content created successfully for file: {FileName}", request.OriginalFileName);
                return Ok(new
                {
                    FileName = request.OriginalFileName,
                    ContentType = request.ContentType,
                    Size = request.Size
                });
                
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred in PostAsync method.");
                Console.WriteLine("❌ Exception: " + ex.Message);
                Console.WriteLine("❌ Stack Trace: " + ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        // Delete a product by id
        [HttpDelete("{id}")]
        public IActionResult Delete(string name)
        {
            Log.Information("DELETE request received for name: {Name}", name);
            try
            {
                var result = _contentService.DeleteContent(name);
                Log.Information("DeleteContent result for name {Name}: {Result}", name, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred in Delete method for name: {Name}", name);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }


    }
}
