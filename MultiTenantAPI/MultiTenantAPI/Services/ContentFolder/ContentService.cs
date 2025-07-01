using Microsoft.EntityFrameworkCore;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.RabbitMQ;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;


namespace MultiTenantAPI.Services.ContentFolder
{       

    public class ContentService : IContentService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IBlobService _blobClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ContentService> _logger;
        private readonly IRabbitMqPublisherService _publisher;




        public ContentService(
            AppDbContext context,
            ICurrentTenantService currentTenantService,
            IBlobServiceFactory factory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ContentService> logger,
            IRabbitMqPublisherService publisher)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _blobClient = factory.GetClient();
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _publisher = publisher;
        }

        // Get a list of all contents
        public IEnumerable<ContentDto> GetAllContent()
        {
            var tenantId = _currentTenantService.TenantId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");

            _logger.LogInformation("Fetching up to 50 content items for tenant {TenantId} and user {UserId}.", tenantId, userId);

            if (tenantId.HasValue == false)
            {
                throw new Exception("Tenant ID is not set. Please ensure the tenant is initialized.");
            }

            // Parse userId as Guid
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError("Invalid userID format: {UserId}", userId);
                throw new Exception("Invalid userID format.");
            }

            var result = _context.Contents
                .Where(c => c.TenantID == tenantId &&
                            (!c.IsPrivate || c.UserId == userGuid))
                .Take(50)
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    ContentType = c.ContentType,
                    Size = c.Size,
                    TenantId = c.TenantID,
                    thumbnail = c.thumbnail,
                    UserId = c.UserId,
                    Status = c.Status
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

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError("Invalid userID format: {UserId}", userId);
                throw new Exception("Invalid userID format.");
            }

            var result = _context.Contents
                .Where(c => c.TenantID == tenantId &&
                            (!c.IsPrivate || c.UserId == userGuid) &&
                            c.FileName.ToLower().Contains(name.ToLower()))
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    ContentType = c.ContentType,
                    Size = c.Size,
                    TenantId = c.TenantID,
                    thumbnail = c.thumbnail,
                    UserId = c.UserId,
                    Status = c.Status
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

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError("Invalid userID format: {UserId}", userId);
                throw new Exception("Invalid userID format.");
            }
            var content = _context.Contents.FirstOrDefault(x =>
                x.Id == id &&
                x.TenantID == _currentTenantService.TenantId &&
                (!x.IsPrivate || x.UserId == userGuid));

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

        public object GenerateUploadUrlAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.");

            var blobFileName = $"pending-{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var uploadUrl = _blobClient.GetUploadUrl(blobFileName);


            return new
            {
                uploadUrl,
                blobFileName,
            };
        }



        // Create a new content
        public async Task<ContentDto> CreateContent(CreateProductRequest request)
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var userId = user?.Claims.FirstOrDefault(x => x.Type == "userID")?.Value;
                var tenantId = _currentTenantService.TenantId;
                if (tenantId == null)
                {
                    _logger.LogError("Tenant ID is not set. Please ensure the tenant is initialized.");
                    throw new Exception("Tenant ID is not set. Please ensure the tenant is initialized.");
                }
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("User ID not found in claims.");
                    throw new Exception("User ID not found in claims.");
                }
                // Convert userId string to Guid
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    _logger.LogError("Invalid userID format: {UserId}", userId);
                    throw new Exception("Invalid userID format.");
                }
                



                var message = new ContentMessage
                {
                    FileName = request.OriginalFileName,
                    ContentType = request.ContentType,
                    Size = request.Size,
                    UserId = userId,
                    TenantId = (Guid)tenantId,
                    uniqueFileName = request.BlobFileName,
                    isPrivate = request.IsPrivate,
                    RequiredRendition = request.Rendition
                };

                

                await _publisher.PublishMessageAsync (tenantId.Value.ToString(), message);

                using var transaction = await _context.Database.BeginTransactionAsync();

                var content = new Content
                {
                    TenantID = message.TenantId,
                    FileName = message.FileName,
                    ContentType = message.ContentType,
                    Size = message.Size,
                    FilePath = message.uniqueFileName,
                    thumbnail = null,
                    UserId = userGuid,
                    IsPrivate = message.isPrivate,
                    Status = false // Will turn to true once processing is complete
                };

                _context.Contents.Add(content);
                int affectedRows = await _context.SaveChangesAsync();
                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("SaveChangesAsync returned 0. No rows affected.");
                    throw new Exception("No rows were affected while saving content.");
                }
                await transaction.CommitAsync();

                _logger.LogInformation("Content created successfully: Id={Id}, FileName={FileName}, ContentType={ContentType}, Size={Size}, FilePath={FilePath}, TenantId={TenantId}, UserId={UserId}, IsPrivate={IsPrivate}, Status={Status}",
                    content.Id,
                    content.FileName,
                    content.ContentType,
                    content.Size,
                    content.FilePath,
                    content.TenantID,
                    content.UserId,
                    content.IsPrivate,
                    content.Status
                );

                // Map the Content object to a ContentDto object before returning
                var contentDto = new ContentDto
                {
                    Id = content.Id,
                    FileName = content.FileName,
                    ContentType = content.ContentType,
                    Size = content.Size,
                    TenantId = content.TenantID,
                    thumbnail = "",
                    UserId = content.UserId,
                    Status = content.Status
                };

                return contentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the content.");
                throw new Exception("An error occurred while creating the content.", ex);
            }
        }


        public async Task<bool> DeleteContent(string name)
        {
            _logger.LogInformation("Attempting to delete content with name: {Name}", name);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userID");

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError("Invalid userID format: {UserId}", userId);
                throw new Exception("Invalid userID format.");
            }

            var content = _context.Contents.FirstOrDefault(x => x.FileName == name && x.UserId == userGuid);

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

        public async Task<bool> DeleteUserContentAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError("Invalid userID format: {UserId}", userId);
                throw new Exception("Invalid userID format.");
            }

            var contents = await _context.Contents
                .Where(c => c.UserId == userGuid)
                .ToListAsync();

            if (contents.Count == 0) return true;

            var tenantId = _currentTenantService.TenantId;
            if (tenantId == null)
                return false;

            foreach (var content in contents)
            {
                if (!string.IsNullOrWhiteSpace(content.FilePath))
                {
                    var blobName = Path.GetFileName(content.FilePath); 
                    try
                    {
                        await _blobClient.DeleteBlobAsync(blobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete blob: {BlobName} ", blobName);

                        return false;
                    }
                }
            }

            // No need to call _context.Contents.RemoveRange because of cascade delete
            return true;
        }



    }
}
