using Microsoft.EntityFrameworkCore;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.ContentHandler;
using MultiTenantAPI.Services.FFmpeg.Converter;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;
using MultiTenantAPI.Services.ProgressStore;

namespace MultiTenantAPI.Services.ContentProcessor
{
    public class ContentProcessorService : IContentProcessorService
    {
        private readonly ILogger<ContentProcessorService> _logger;
        private readonly AppDbContext _context;
        private readonly IBlobService _blobClient;
        private readonly IProgressStore _progressStore;
        private readonly IEnumerable<IContentHandler> _handlers;

        public ContentProcessorService(
        AppDbContext context,
        IBlobServiceFactory factory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ContentProcessorService> logger,
        IProgressStore progressStore,
        IEnumerable<IContentHandler> contentHandlers)
        {
            _context = context;
            _blobClient = factory.GetClient();
            _logger = logger;
            _progressStore = progressStore;
            _handlers = contentHandlers;
        }



        public async Task ProcessUploadedContentAsync(ContentMessage message)
        {
            string userId = message.UserId;
            string originalFileName = message.FileName;
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            string uniqueFileName = message.uniqueFileName;
            string? blobClientUri = null;

            var content = await _context.Contents.FirstOrDefaultAsync(c => c.FilePath == uniqueFileName);
            if (content == null)
            {
                _logger.LogWarning("Content not found: FileName={FileName}, TenantId={TenantId}", originalFileName, message.TenantId);
                return;
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation("Uploads folder does not exist. Creating: {UploadsFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }

            string tempFilePath = Path.Combine(uploadsFolder, $"__temp_{Guid.NewGuid()}{extension}");

            try
            {
                _progressStore.SetProgress(userId, 10);
                _logger.LogInformation("Downloading blob to temp file...");

                await using var blobStream = await _blobClient.GetBlobAsync(uniqueFileName);
                await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await blobStream.CopyToAsync(fileStream);
                }

                var handler = _handlers.FirstOrDefault(h => h.CanHandle(extension));
                if (handler == null)
                    throw new NotSupportedException($"Unsupported file type: {extension}");

                _logger.LogInformation("Processing file using {HandlerType}", handler.GetType().Name);
                var result = await handler.ProcessAsync(message, tempFilePath);

                _progressStore.SetProgress(userId, 70);

                // Remove "pending-" prefix if exists
                string finalFileName = result.FinalFileName;
                if (finalFileName.StartsWith("pending-"))
                    finalFileName = finalFileName.Substring("pending-".Length);

                await using var uploadStream = File.OpenRead(result.ProcessedFilePath);
                blobClientUri = await _blobClient.UploadAsync(uploadStream, finalFileName, message.ContentType);

                if (string.IsNullOrEmpty(blobClientUri))
                    throw new Exception("Failed to upload file to blob storage.");

                using var transaction = await _context.Database.BeginTransactionAsync();

                content.thumbnail = Path.GetFileName(result.ThumbnailPath);
                content.Status = true;
                content.FilePath = finalFileName;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Content successfully processed and database updated.");

                var deleted = await _blobClient.DeleteBlobAsync(uniqueFileName);
                if (!deleted)
                    throw new Exception("Failed to delete pending blob file");

                _progressStore.SetProgress(userId, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing content for user {UserId}", userId);

                if (!string.IsNullOrEmpty(blobClientUri))
                {
                    try
                    {
                        await _blobClient.DeleteBlobAsync(uniqueFileName);
                        _logger.LogInformation("Rolled back uploaded blob: {FileName}", uniqueFileName);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to rollback uploaded blob: {FileName}", uniqueFileName);
                    }
                }

                throw;
            }
            finally
            {
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }



    }
}
