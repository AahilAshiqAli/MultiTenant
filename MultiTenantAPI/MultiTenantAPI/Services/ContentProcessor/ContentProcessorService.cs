using Azure.Storage.Blobs;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.FFmpeg.Converter;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;

namespace MultiTenantAPI.Services.ContentProcessor
{
    public class ContentProcessorService : IContentProcessorService
    {
        private readonly ILogger<ContentProcessorService> _logger;
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IBlobService _blobClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProgressStore _progressStore;
        private readonly IConverterService _converterService;
        private readonly IVideoRenditionService _videoRenditionService;
        private readonly IThumbnailService _thumbnailService;

        public ContentProcessorService(
        AppDbContext context,
        ICurrentTenantService currentTenantService,
        IBlobServiceFactory factory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ContentProcessorService> logger,
        IProgressStore progressStore,
        IConverterService converterService,
        IVideoRenditionService videoRenditionService,
        IThumbnailService thumbnailService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _blobClient = factory.GetClient();
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _progressStore = progressStore;
            _converterService = converterService;
            _thumbnailService = thumbnailService;
            _videoRenditionService = videoRenditionService;
        }



        public async Task ProcessUploadedContentAsync(ContentMessage message)
        {

            string userId = message.UserId;
            string originalFileName = message.FileName;
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            string uniqueFileName = message.uniqueFileName;
            string thumbnail = "";
            string blobClientUri = null;
            string RequiredRendition = message.RequiredRendition;


            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation("Uploads folder does not exist. Creating: {UploadsFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }

            var tempFilePath = Path.Combine(uploadsFolder, $"__temp_{Guid.NewGuid()}{extension}");

            try
            {
                _progressStore.SetProgress(userId, 10);
                _logger.LogInformation("Sent conversion progress 10% to user {UserId}.", userId);

                // Download the blob to local temp file
                await using var blobStream = await _blobClient.GetBlobAsync(uniqueFileName);
                await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await blobStream.CopyToAsync(fileStream);
                }

                string processedFilePath = tempFilePath;

                var audioExtensions = new[] { ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma" };
                var videoExtensions = new[] { ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };

                if (audioExtensions.Contains(extension))
                {
                    processedFilePath = await _converterService.ConvertToMp3Async(tempFilePath);
                    if (string.IsNullOrEmpty(processedFilePath))
                        throw new Exception("Failed to convert audio file.");

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp3");
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                }
                else if (videoExtensions.Contains(extension) || extension == ".mp4")
                {
                    processedFilePath = await _converterService.ConvertToMp4Async(tempFilePath);
                    if (string.IsNullOrEmpty(processedFilePath))
                        throw new Exception("Failed to convert video file.");

                    uniqueFileName = Path.ChangeExtension(uniqueFileName, ".mp4");

                    var rendition = await _videoRenditionService.GetRenditionLabelAsync(processedFilePath, RequiredRendition);
                    if (!rendition)
                    {
                        _logger.LogError("Rendition label not met. Aborting.");
                        return;
                    }

                    thumbnail = Path.Combine(uploadsFolder, Path.ChangeExtension(uniqueFileName, ".jpg"));
                    bool success = await _thumbnailService.ExtractThumbnailAsync(processedFilePath, thumbnail);
                    if (!success || !File.Exists(thumbnail))
                        throw new Exception("Failed to generate video thumbnail.");

                    processedFilePath = await _videoRenditionService.GenerateVideoRenditionsAsync(processedFilePath, RequiredRendition);
                    uniqueFileName = AddRenditionToFileName(uniqueFileName, RequiredRendition);
                }
                else if (extension == ".mp3")
                {
                    thumbnail = Path.Combine(uploadsFolder, "music.jpg");
                }
                else
                {
                    throw new NotSupportedException($"File type '{extension}' is not supported.");
                }

                _progressStore.SetProgress(userId, 70);
                _logger.LogInformation("Sent conversion progress 70% to user {UserId}.", userId);

                await using var uploadStream = File.OpenRead(processedFilePath);
                blobClientUri = await _blobClient.UploadAsync(uploadStream, uniqueFileName, message.ContentType);

                if (string.IsNullOrEmpty(blobClientUri))
                    throw new Exception("Failed to upload file to blob storage.");

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

                _progressStore.SetProgress(userId, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during content processing for user {UserId}", userId);

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

                throw;
            }
            finally
            {
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }

        string AddRenditionToFileName(string uniqueFileName, string rendition)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(uniqueFileName); // "aahil"
            var extension = Path.GetExtension(uniqueFileName);                            // ".jpg"
            return $"{nameWithoutExtension}{rendition}{extension}";                       // "aahil_480p.jpg"
        }

    }
}
