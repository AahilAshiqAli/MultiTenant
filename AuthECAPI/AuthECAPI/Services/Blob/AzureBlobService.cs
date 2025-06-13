using AuthECAPI.Models;
using AuthECAPI.Services.CurrentTenant;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;


namespace AuthECAPI.Services.Blob
{
    public class AzureBlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ICurrentTenantService _currentTenantService;

        public AzureBlobService(BlobServiceClient blobServiceClient,ICurrentTenantService currentTenantService, AppDbContext dbContext )
        {
            _currentTenantService = currentTenantService;
            var CurrentTenantId = _currentTenantService.TenantId;

            var tenantId = _currentTenantService.TenantId;

            var tenant = dbContext.Tenants.FirstOrDefault(t => t.TenantID.ToString() == tenantId);
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Container))
            {
                throw new Exception($"Tenant or container not found for TenantId: {tenantId}");
            }
            _containerClient = blobServiceClient.GetBlobContainerClient(tenant.Container);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(fileName);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload blob '{fileName}': {ex.Message}", ex);
            }
        }

        public async Task<Stream> GetBlobAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<IEnumerable<string>> ListBlobsAsync()
        {
            var blobs = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }

        public async Task<bool> DeleteBlobAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var result = await blobClient.DeleteIfExistsAsync();
            return result.Value; // true if deleted, false if blob did not exist
        }
    }
}
