using MultiTenantAPI.DTO;
using MultiTenantAPI.Hubs;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.Converter;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.RabbitMQ;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MultiTenantAPI.Services.ContentFolder
{       

    public class ContentService : IContentService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IBlobService _blobClient;
        private readonly IFFmpegService _ffmpegService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ContentService> _logger;
        private readonly IRabbitMqPublisherService _publisher;
        private readonly IProgressStore _progressStore;



        public ContentService(
            AppDbContext context,
            ICurrentTenantService currentTenantService,
            IBlobServiceFactory factory,
            IFFmpegService fFmpegService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ContentService> logger,
            IRabbitMqPublisherService publisher,
            IProgressStore progressStore)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _blobClient = factory.GetClient();
            _ffmpegService = fFmpegService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _publisher = publisher;
            _progressStore = progressStore;
        }

        // Get a list of all contents
        public IEnumerable<ContentDto> GetAllContent()
        {
            var tenantId = _currentTenantService.TenantId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");

            _logger.LogInformation("Fetching up to 50 content items for tenant {TenantId} and user {UserId}.", tenantId, userId);

            var result = _context.Contents
                .Where(c => c.TenantID == tenantId &&
                            (!c.IsPrivate || c.UserId == userId))
                .Take(50)
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    ContentType = c.ContentType,
                    Size = c.Size,
                    TenantId = c.TenantID,
                    thumbnail = c.thumbnail,
                    UserId = c.UserId
                })
                .ToList();

            _logger.LogInformation("Fetched {Count} content items after filtering.", result.Count);
            return result;
        }


        // Get a list of contents filtered by name
        public IEnumerable<ContentDto> FilterContentByName(string name)
        {
            var tenantId = _currentTenantService.TenantId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");

            _logger.LogInformation("Filtering content by name: {Name} for tenant {TenantId} and user {UserId}", name, tenantId, userId);

            var result = _context.Contents
                .Where(c => c.TenantID == tenantId &&
                            (!c.IsPrivate || c.UserId == userId) &&
                            c.FileName.ToLower().Contains(name.ToLower()))
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    ContentType = c.ContentType,
                    Size = c.Size,
                    TenantId = c.TenantID,
                    thumbnail = c.thumbnail,
                    UserId = c.UserId
                })
                .ToList();

            _logger.LogInformation("Found {Count} content items matching filter '{Name}' after tenant and privacy filtering.", result.Count, name);
            return result;
        }


        // Get a single content stream by Id
        public Task<string> StreamVideoAsync(int id)
        {
            _logger.LogInformation("Attempting to stream video with Id: {Id}", id);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");

            var content = _context.Contents.FirstOrDefault(x =>
                x.Id == id &&
                x.TenantID == _currentTenantService.TenantId &&
                (!x.IsPrivate || x.UserId == userId));

            if (content == null)
            {
                _logger.LogWarning("Video with Id {Id} not found.", id);
                throw new KeyNotFoundException("Video not found.");
            }

            _logger.LogInformation("Found video: {FileName}, Size: {Size}, ContentType: {ContentType}",
                content.FileName, content.Size, content.ContentType);

            var sasUri = _blobClient.GenerateSasUrl(content.FilePath);

            _logger.LogInformation("Generated SAS URL for video Id {Id}: {SasUri}", id, sasUri);

            return Task.FromResult(sasUri);
        }



        // Create a new content
        public async Task CreateContent(CreateProductRequest request)
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

                var message = new ContentMessage
                {
                    FilePath = filePath,
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    UserId = userId,
                    TenantId = _currentTenantService.TenantId,
                    uploadsFolder = uploadsFolder,
                    dupFilePath = dupFilePath,
                    uniqueFileName = uniqueFileName,
                    isPrivate = request.IsPrivate
                };

                await _publisher.PublishMessageAsync (_currentTenantService.TenantId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the content.");
                throw new Exception("An error occurred while creating the content.", ex);
            }
        }


        public async Task ProcessUploadedContentAsync(ContentMessage message)
        {
            
            string userId = message.UserId;
            string filePath = message.FilePath;
            string originalFileName = message.FileName;
            string uploadsFolder = message.uploadsFolder;
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            string uniqueFileName = message.uniqueFileName;
            string thumbnail = "";
            string dupFilePath = message.dupFilePath;
            string blobClientUri = null;

            try { 
                // store status as 10% complete
                _progressStore.SetProgress(userId, 10);
                _logger.LogInformation("Sent conversion progress 10% to user {UserId}.", userId);

                var audioExtensions = new[] { ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma" };
                var videoExtensions = new[] { ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };

                if (audioExtensions.Contains(extension))
                {
                    _logger.LogInformation("Audio file detected. Converting to mp3.");
                    filePath = await _ffmpegService.ConvertToMp3Async(filePath);
                    if (string.IsNullOrEmpty(filePath))
                        throw new Exception("Failed to convert audio file.");

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp3");
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                }
                else if (videoExtensions.Contains(extension))
                {
                    _logger.LogInformation("Video file detected. Converting to mp4.");
                    filePath = await _ffmpegService.ConvertToMp4Async(filePath);
                    if (string.IsNullOrEmpty(filePath))
                        throw new Exception("Failed to convert video file.");

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp4");
                    thumbnail = Path.Combine(uploadsFolder, Path.ChangeExtension(uniqueFileName, ".jpg"));

                    _logger.LogInformation("Extracting thumbnail for video.");
                    bool success = await _ffmpegService.ExtractThumbnailAsync(filePath, thumbnail);
                    if (!success || !System.IO.File.Exists(thumbnail))
                        throw new Exception("Failed to generate video thumbnail.");
                }
                else if (extension == ".mp3")
                {
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, extension);
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                }
                else if (extension == ".mp4")
                {
                    uniqueFileName = Path.ChangeExtension(uniqueFileName, extension);
                    thumbnail = Path.Combine(uploadsFolder, Path.ChangeExtension(uniqueFileName, ".jpg"));

                    bool success = await _ffmpegService.ExtractThumbnailAsync(filePath, thumbnail);
                    if (!success)
                        _logger.LogWarning("Failed to extract thumbnail for MP4 file: {FilePath}", filePath);
                }
                else
                {
                    throw new NotSupportedException($"File type '{extension}' is not supported.");
                }

                // store styus as 50% complete
                _progressStore.SetProgress(userId,70);
                _logger.LogInformation("Sent conversion progress 70% to user {UserId}.", userId);

                using (var stream = File.OpenRead(filePath))
                {
                    blobClientUri = await _blobClient.UploadAsync(
                        stream,
                        uniqueFileName,
                        message.ContentType
                    );
                }

                if (string.IsNullOrEmpty(blobClientUri))
                    throw new Exception("Failed to upload file to blob storage.");
                await Task.Delay(1000);


                // implementing Transaction For RollBack
                // Before it, we should make sure that everything is good else we should perform rollback of previous things and end.

                using var transaction = await _context.Database.BeginTransactionAsync();


                var content = new Content
                {
                    TenantID = message.TenantId,
                    FileName = originalFileName,
                    ContentType = message.ContentType,
                    Size = message.Size,
                    FilePath = uniqueFileName,
                    thumbnail = Path.GetFileName(thumbnail),
                    UserId = userId,
                    IsPrivate = message.isPrivate
                };

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Content created successfully: Id={Id}, FileName={FileName}, ContentType={ContentType}, Size={Size}, FilePath={FilePath}, TenantId={TenantId}, UserId={UserId}, IsPrivate={IsPrivate}, Thumbnail={Thumbnail}",
                    content.Id,
                    content.FileName,
                    content.ContentType,
                    content.Size,
                    content.FilePath,
                    content.TenantID,
                    content.UserId,
                    content.IsPrivate,  
                    content.thumbnail
                );


                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Deleting local file: {FilePath}", filePath);
                    File.Delete(filePath);
                    if (File.Exists(dupFilePath)) File.Delete(dupFilePath);
                }
                    // store status as 100% complete
                    _progressStore.SetProgress(userId, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during content processing for user {UserId}", userId);

                // Rollback manually uploaded blob if needed
                if (!string.IsNullOrEmpty(blobClientUri))
                {
                    try
                    {
                        await _blobClient.DeleteBlobAsync(uniqueFileName);
                        _logger.LogInformation("Rolled back uploaded blob: {FileName}", uniqueFileName);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to rollback uploaded blob for file: {FileName}", uniqueFileName);
                    }
                }

                // Clean up local files
                if (File.Exists(filePath)) File.Delete(filePath);
                if (File.Exists(dupFilePath)) File.Delete(dupFilePath);

                throw; // Let the caller know an error happened
            }
        }

        // Delete content
        public async Task<bool> DeleteContent(string name)
        {
            _logger.LogInformation("Attempting to delete content with name: {Name}", name);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");
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
