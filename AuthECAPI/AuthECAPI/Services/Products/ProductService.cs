using AuthECAPI.DTO;
using AuthECAPI.Hubs;
using AuthECAPI.Models;
using AuthECAPI.Services.Blob;
using AuthECAPI.Services.Converter;
using AuthECAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AuthECAPI.Services.Products
{

    public class ProductService : IProductService
    {
        private readonly AppDbContext _context; // database context
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IBlobService _blobClient;
        private readonly IFFmpegService _ffmpegService;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(AppDbContext context, ICurrentTenantService currentTenantService, IBlobServiceFactory factory, IFFmpegService fFmpegService, IHubContext<ProgressHub> hubContext, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _blobClient = factory.GetClient();
            _ffmpegService = fFmpegService;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        // get a list of all products
        public IEnumerable<Product> GetAllProducts()
        {
            var products = _context.Products.Take(50).ToList();
            return products;
        }

        // get a list of products by name filtered by name
        public IEnumerable<Product> FilterProductsByName(string name)
        {
            return _context.Products
                .Where(p => p.FileName.Contains(name))
                .ToList();
        }

        // get a single product
        public async Task<Stream> GetProductByName(string name)
        {
            var product = _context.Products.FirstOrDefault(x => x.FileName == name);

            if (product == null)
                throw new KeyNotFoundException($"Product with file name '{name}' not found.");

            return await _blobClient.GetBlobAsync(product.FilePath);

        }

        // create a new product
        public async Task<Product> CreateProduct(CreateProductRequest request)
        {
            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(request.File.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                var dupFilePath = filePath;

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                     // file stream is used to write on disk. Memory stream is used to write in RAM
                    await request.File.CopyToAsync(fileStream);
                }

                var user = _httpContextAccessor.HttpContext?.User;
                var userId = user?.Claims.FirstOrDefault(x => x.Type == "userID")?.Value;
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User ID not found in claims.");

                await _hubContext.Clients.User(userId).SendAsync("ConversionProgress", 10);
                // Or we can do it with MME types using FileExtensionContentProvider as well
                var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                var audioExtensions = new[] { ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma" };
                var videoExtensions = new[] { ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };

                if (audioExtensions.Contains(extension))
                {
                    filePath = await _ffmpegService.ConvertToMp3Async(filePath);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        throw new Exception("Failed to convert audio file.");
                    }
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp3");
                }
                else if (videoExtensions.Contains(extension))
                {
                    filePath = await _ffmpegService.ConvertToMp4Async(filePath);
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp4");
                    if (string.IsNullOrEmpty(filePath))
                    {
                        throw new Exception("Failed to convert video file.");
                    }
                }
                else if (extension == ".mp3" || extension == ".mp4")
                {
                    // No conversion needed for mp3 or mp4 files
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, extension);
                }
                else
                {
                    throw new NotSupportedException($"File type '{extension}' is not supported for conversion. Supported types are: {string.Join(", ", audioExtensions.Concat(videoExtensions))}.");
                }
                await _hubContext.Clients.User(userId).SendAsync("ConversionProgress", 70);
                Console.WriteLine($"File converted and saved as: {uniqueFileName}");
                var blobClientUri = await _blobClient.UploadAsync(request.File.OpenReadStream(), uniqueFileName, request.File.ContentType);

                if (string.IsNullOrEmpty(blobClientUri))
                {
                    throw new Exception("Failed Upload Exception");
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    File.Delete(dupFilePath); // deleting input video
                    Console.WriteLine("File deleted.");
                }
                else
                {
                    Console.WriteLine("File not found.");
                }


                Console.WriteLine($"File uploaded to blob storage: {blobClientUri}");

                var product = new Product
                {
                    TenantId = _currentTenantService.TenantId,
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    FilePath = uniqueFileName
                };

                _context.Products.Add(product);
                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    Console.WriteLine("No changes were saved.");
                    
                }



                return product;
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                throw new Exception("An error occurred while creating the product.", ex);
            }
        }


        // delete a product
        public async Task<bool> DeleteProduct(string name)
        {
            var product = _context.Products.FirstOrDefault(x => x.FileName == name);

            if (product != null)
            {
                bool result = await _blobClient.DeleteBlobAsync(product.FilePath);
                if (!result)
                {
                    throw new Exception($"Failed to delete blob for product '{name}'.");
                }
                _context.Products.Remove(product);
                _context.SaveChanges();
                return true;
            }

            return false;
        }
    }
}
