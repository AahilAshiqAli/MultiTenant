using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.CurrentTenant;

namespace MultiTenantAPI.Services.Blob
{
    public class AzureBlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly ILogger<AzureBlobService> _logger;
        private readonly string _accessTier;

        public AzureBlobService(
            BlobServiceClient blobServiceClient,
            ICurrentTenantService currentTenantService,
            AppDbContext dbContext,
            ILogger<AzureBlobService> logger)
        {
            _logger = logger;
            _currentTenantService = currentTenantService;
            var tenantId = _currentTenantService.TenantId;
            _logger.LogInformation("Initializing AzureBlobService for TenantId: {TenantId}", tenantId);

            var tenant = dbContext.Tenants.FirstOrDefault(t => t.TenantID.ToString() == tenantId);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Container))
            {
                _logger.LogError("Tenant or container not found for TenantId: {TenantId}", tenantId);
                throw new Exception($"Tenant or container not found for TenantId: {tenantId}");
            }
            _containerClient = blobServiceClient.GetBlobContainerClient(tenant.Container);
            _accessTier = tenant.DefaultBlobTier;
            _logger.LogInformation("Obtained BlobContainerClient for container: {Container}", tenant.Container);
            _containerClient.CreateIfNotExists();
            _logger.LogInformation("Ensured container exists: {Container}", tenant.Container);
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
        {
            _logger.LogInformation("Uploading blob. FileName: {FileName}, ContentType: {ContentType}", fileName, contentType);
            try
            {
                var blobClient = _containerClient.GetBlobClient(fileName);
                _logger.LogDebug("Got BlobClient for file: {FileName}", fileName);
                var accessTier = _accessTier switch
                {
                    "Hot" => AccessTier.Hot,
                    "Cool" => AccessTier.Cool,
                    "Archive" => AccessTier.Archive,
                    _ => AccessTier.Hot // default to Hot if invalid input
                };

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    },
                    AccessTier = accessTier
                };

                await blobClient.UploadAsync(stream, uploadOptions);
                _logger.LogInformation("Successfully uploaded blob: {FileName} to {Uri}", fileName, blobClient.Uri);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload blob '{FileName}'", fileName);
                throw new Exception($"Failed to upload blob '{fileName}': {ex.Message}", ex);
            }
        }


        // Downloads the video in chunks based on provided range or the full blob if no range is specified.
        public async Task<Stream> GetBlobAsync(string fileName, long? start = null, long? end = null)
        {
            _logger.LogInformation("Getting blob. FileName: {FileName}, Start: {Start}, End: {End}", fileName, start, end);
            var blobClient = _containerClient.GetBlobClient(fileName);
            try
            {
                if (start.HasValue && end.HasValue)
                {
                    var length = end.Value - start.Value + 1;
                    var httpRange = new HttpRange(start.Value, length);
                    _logger.LogDebug("Downloading blob range. FileName: {FileName}, Range: {Start}-{End}", fileName, start, end);
                    var downloadResponse = await blobClient.DownloadAsync(range: httpRange);
                    _logger.LogInformation("Successfully downloaded blob range for {FileName}", fileName);
                    return downloadResponse.Value.Content;
                }

                _logger.LogDebug("Downloading full blob. FileName: {FileName}", fileName);
                var fullResponse = await blobClient.DownloadAsync();
                _logger.LogInformation("Successfully downloaded full blob for {FileName}", fileName);
                return fullResponse.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get blob '{FileName}'", fileName);
                throw new Exception($"Failed to get blob '{fileName}': {ex.Message}", ex);
            }
        }

        // generates SaS URL for the blob with read permissions, valid for 15 minutes.
        public string GenerateSasUrl(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException("SAS URI cannot be generated for this blob.");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogInformation("Generated SAS URI for blob {FileName}", fileName);
            return sasUri.ToString();
        }

        public async Task<IEnumerable<string>> ListBlobsAsync()
        {
            _logger.LogInformation("Listing blobs in container: {Container}", _containerClient.Name);
            var blobs = new List<string>();
            try
            {
                await foreach (var blobItem in _containerClient.GetBlobsAsync())
                {
                    _logger.LogDebug("Found blob: {BlobName}", blobItem.Name);
                    blobs.Add(blobItem.Name);
                }
                _logger.LogInformation("Successfully listed {Count} blobs in container: {Container}", blobs.Count, _containerClient.Name);
                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list blobs in container: {Container}", _containerClient.Name);
                throw new Exception($"Failed to list blobs: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteBlobAsync(string fileName)
        {
            _logger.LogInformation("Deleting blob. FileName: {FileName}", fileName);
            var blobClient = _containerClient.GetBlobClient(fileName);
            try
            {
                var result = await blobClient.DeleteIfExistsAsync();
                if (result.Value)
                {
                    _logger.LogInformation("Successfully deleted blob: {FileName}", fileName);
                }
                else
                {
                    _logger.LogWarning("Blob not found or already deleted: {FileName}", fileName);
                }
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete blob '{FileName}'", fileName);
                throw new Exception($"Failed to delete blob '{fileName}': {ex.Message}", ex);
            }
        }
    }
}
