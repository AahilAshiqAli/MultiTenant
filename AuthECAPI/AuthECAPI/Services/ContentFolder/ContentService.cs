using AuthECAPI.DTO;
using AuthECAPI.Hubs;
using AuthECAPI.Models;
using AuthECAPI.Services.Blob;
using AuthECAPI.Services.Converter;
using AuthECAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthECAPI.Services.ContentFolder
{       

    public class ContentService : IContentService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IBlobService _blobClient;
        private readonly IFFmpegService _ffmpegService;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ContentService> _logger;

        public ContentService(
            AppDbContext context,
            ICurrentTenantService currentTenantService,
            IBlobServiceFactory factory,
            IFFmpegService fFmpegService,
            IHubContext<ProgressHub> hubContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ContentService> logger)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _blobClient = factory.GetClient();
            _ffmpegService = fFmpegService;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Get a list of all contents
        public IEnumerable<ContentDto> GetAllContent()
        {
            _logger.LogInformation("Fetching up to 50 content items from the database.");
            var result = _context.Contents.Take(50).Select(c => new ContentDto
            {
                Id = c.Id,
                FileName = c.FileName,
                ContentType = c.ContentType,
                Size = c.Size,
                TenantId = c.TenantId,
                thumbnail = c.thumbnail,
                UserId = c.UserId
            }).ToList();
            _logger.LogInformation("Fetched {Count} content items.", result.Count);
            return result;
        }

        // Get a list of contents filtered by name
        public IEnumerable<ContentDto> FilterContentByName(string name)
        {
            _logger.LogInformation("Filtering content by name: {Name}", name);
            var result = _context.Contents
                .Where(c => c.FileName.ToLower().Contains(name.ToLower()))
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    ContentType = c.ContentType,
                    Size = c.Size,
                    TenantId = c.TenantId,
                    thumbnail = c.thumbnail,
                    UserId = c.UserId
                })
                .ToList();
            _logger.LogInformation("Found {Count} content items matching filter '{Name}'.", result.Count, name);
            return result;
        }

        // Get a single content stream by Id
        public async Task<IActionResult> StreamVideoAsync(int id, HttpRequest request, HttpResponse response)
        {
            _logger.LogInformation("Attempting to stream video with Id: {Id}", id);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var content = _context.Contents.FirstOrDefault(x => x.Id == id && x.UserId == userId);
            if (content == null)
            {
                _logger.LogWarning("Video with Id {Id} not found.", id);
                throw new KeyNotFoundException("Video not found.");
            }
            _logger.LogInformation("Found video: {FileName}, Size: {Size}, ContentType: {ContentType}", content.FileName, content.Size, content.ContentType);
            var blobClient = _blobClient;
            long fileLength = content.Size;

            request.Headers.TryGetValue("Range", out var rangeHeader);
            var contentType = content.ContentType ?? "video/mp4";

            if (!string.IsNullOrEmpty(rangeHeader))
            {
                _logger.LogInformation("Range header detected: {RangeHeader}", rangeHeader);
                var range = rangeHeader.ToString().Replace("bytes=", "").Split('-');
                long start = long.Parse(range[0]);
                long end = (range.Length > 1 && !string.IsNullOrEmpty(range[1]))
                    ? long.Parse(range[1])
                    : fileLength - 1;

                _logger.LogInformation("Streaming partial content: {Start}-{End} of {FileLength}", start, end, fileLength);
                var stream = await blobClient.GetBlobAsync(content.FilePath, start, end);

                response.StatusCode = 206; // Partial Content
                response.Headers.Append("Content-Range", $"bytes {start}-{end}/{fileLength}");
                response.Headers.Append("Accept-Ranges", "bytes");
                response.ContentLength = end - start + 1;

                _logger.LogInformation("Partial content stream ready for video Id {Id}.", id);
                return new FileStreamResult(stream, contentType);
            }

            _logger.LogInformation("No range header, streaming full content for video Id {Id}.", id);
            var fullStream = await blobClient.GetBlobAsync(content.FilePath);
            response.Headers.Append("Accept-Ranges", "bytes");
            response.ContentLength = fileLength;

            _logger.LogInformation("Full content stream ready for video Id {Id}.", id);
            return new FileStreamResult(fullStream, contentType);
        }

        // Create a new content
        public async Task<Content> CreateContent(CreateProductRequest request)
        {
            try
            {
                _logger.LogInformation("Starting content creation for file: {FileName}, ContentType: {ContentType}, Size: {Size}", request.File.FileName, request.File.ContentType, request.File.Length);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Uploads folder does not exist. Creating: {UploadsFolder}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(request.File.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                var dupFilePath = filePath;
                string thumbnail = "";

                _logger.LogInformation("Saving uploaded file to: {FilePath}", filePath);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(fileStream);
                }

                var user = _httpContextAccessor.HttpContext?.User;
                var userId = user?.Claims.FirstOrDefault(x => x.Type == "userID")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("User ID not found in claims.");
                    throw new Exception("User ID not found in claims.");
                }

                await _hubContext.Clients.User(userId).SendAsync("ConversionProgress", 10);
                _logger.LogInformation("Sent conversion progress 10% to user {UserId}.", userId);

                var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                var audioExtensions = new[] { ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma" };
                var videoExtensions = new[] { ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };

                if (audioExtensions.Contains(extension))
                {
                    _logger.LogInformation("Audio file detected. Converting to mp3.");
                    filePath = await _ffmpegService.ConvertToMp3Async(filePath);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        _logger.LogError("Failed to convert audio file to mp3.");
                        throw new Exception("Failed to convert audio file.");
                    }

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp3");
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                    _logger.LogInformation("Audio conversion complete. New file: {FilePath}", filePath);
                }
                else if (videoExtensions.Contains(extension))
                {
                    _logger.LogInformation("Video file detected. Converting to mp4.");
                    filePath = await _ffmpegService.ConvertToMp4Async(filePath);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        _logger.LogError("Failed to convert video file to mp4.");
                        throw new Exception("Failed to convert video file.");
                    }

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp4");
                    thumbnail = Path.Combine(uploadsFolder, Path.ChangeExtension(uniqueFileName, ".jpg"));
                    _logger.LogInformation("Extracting thumbnail for video.");
                    bool success = await _ffmpegService.ExtractThumbnailAsync(filePath, thumbnail);

                    if (!success || !System.IO.File.Exists(thumbnail))
                    {
                        _logger.LogError("Failed to generate video thumbnail for file: {FilePath}", filePath);
                        throw new Exception("Failed to generate video thumbnail.");
                    }
                    _logger.LogInformation("Video conversion and thumbnail extraction complete.");
                }
                else if (extension == ".mp3")
                {
                    _logger.LogInformation("MP3 file detected. No conversion needed.");
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, extension);
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                }
                else if (extension == ".mp4")
                {
                    _logger.LogInformation("MP4 file detected. Extracting thumbnail.");
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, extension);
                    thumbnail = Path.Combine(uploadsFolder, Path.ChangeExtension(uniqueFileName, ".jpg"));
                    bool success = await _ffmpegService.ExtractThumbnailAsync(filePath, thumbnail);
                    if (!success)
                    {
                        _logger.LogWarning("Failed to extract thumbnail for MP4 file: {FilePath}", filePath);
                    }
                }
                else
                {
                    _logger.LogError("Unsupported file type: {Extension}", extension);
                    throw new NotSupportedException($"File type '{extension}' is not supported for conversion. Supported types: {string.Join(", ", audioExtensions.Concat(videoExtensions))}.");
                }

                await _hubContext.Clients.User(userId).SendAsync("ConversionProgress", 70);
                _logger.LogInformation("Sent conversion progress 70% to user {UserId}.", userId);

                _logger.LogInformation("Uploading file to blob storage: {UniqueFileName}", uniqueFileName);
                var blobClientUri = await _blobClient.UploadAsync(request.File.OpenReadStream(), uniqueFileName, request.File.ContentType);
                if (string.IsNullOrEmpty(blobClientUri))
                {
                    _logger.LogError("Failed to upload file to blob storage: {UniqueFileName}", uniqueFileName);
                    throw new Exception("Failed Upload Exception");
                }

                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Deleting local file: {FilePath}", filePath);
                    File.Delete(filePath);
                    File.Delete(dupFilePath);
                }

                var content = new Content
                {
                    TenantId = _currentTenantService.TenantId,
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    FilePath = uniqueFileName,
                    thumbnail = Path.GetFileName(thumbnail),
                    UserId = userId
                };

                _context.Contents.Add(content);
                int result = await _context.SaveChangesAsync();
                _logger.LogInformation("Content created successfully with Id: {ContentId}", content.Id);

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the content.");
                throw new Exception("An error occurred while creating the content.", ex);
            }
        }

        // Delete content
        public async Task<bool> DeleteContent(string name)
        {
            _logger.LogInformation("Attempting to delete content with name: {Name}", name);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var content = _context.Contents.FirstOrDefault(x => x.FileName == name && x.UserId == userId);

            if (content != null)
            {
                _logger.LogInformation("Found content to delete: {FileName}, Id: {Id}", content.FileName, content.Id);
                bool result = await _blobClient.DeleteBlobAsync(content.FilePath);
                if (!result)
                {
                    _logger.LogError("Failed to delete blob for content '{Name}'.", name);
                    throw new Exception($"Failed to delete blob for content '{name}'.");
                }

                _context.Contents.Remove(content);
                _context.SaveChanges();
                _logger.LogInformation("Content deleted successfully: {FileName}, Id: {Id}", content.FileName, content.Id);
                return true;
            }

            _logger.LogWarning("Content with name '{Name}' not found for deletion.", name);
            return false;
        }
    }
}
