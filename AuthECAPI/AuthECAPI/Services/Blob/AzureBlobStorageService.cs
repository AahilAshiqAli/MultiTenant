using AuthECAPI.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthECAPI.Services.Blob
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AppDbContext _dbContext;

        public AzureBlobStorageService(AppDbContext appDbContext, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("AzureBlobStorageConnectionString");
            _blobServiceClient = new BlobServiceClient(connectionString);
            _dbContext = appDbContext; 
        }

        public async Task<string> CreateTenantContainerAsync(string tenantId, Tenant settings)
        {
            // Validate BlobServiceClient initialization
            if (_blobServiceClient == null)
            {
                throw new InvalidOperationException("Azure BlobServiceClient is not initialized. Check your connection string configuration.");
            }

            // Validate RetentionDays
            if (settings.RetentionDays <= 0)
            {
                throw new ArgumentException("RetentionDays must be a positive integer.", nameof(settings.RetentionDays));
            }

            var containerName = $"tenant-{tenantId.ToLower()}";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            var properties = await _blobServiceClient.GetPropertiesAsync();

            // Always check for nulls and initialize sub-properties if needed
            if (properties.Value == null)
                throw new InvalidOperationException("Failed to retrieve existing Blob service properties.");

            properties.Value.DeleteRetentionPolicy = new BlobRetentionPolicy
            {
                Enabled = true,
                Days = settings.RetentionDays
            };

            await _blobServiceClient.SetPropertiesAsync(properties.Value);

            // Optional: Versioning
            if (settings.EnableVersioning)
            {
                // Versioning is not directly supported in BlobServiceProperties.
                // Instead, you need to enable versioning at the container level.
                var containerProperties = await containerClient.GetPropertiesAsync();
                containerProperties.Value.Metadata["versioning"] = "enabled";
                await containerClient.SetMetadataAsync(containerProperties.Value.Metadata);
            }

            return containerName;
        }

        public async Task<IEnumerable<string>> ListAllContainersAsync()
        {
            return await _dbContext.Tenants
                  .IgnoreQueryFilters() 
                  .Select(t => t.Container)
                  .ToListAsync();
        }
    }

}
