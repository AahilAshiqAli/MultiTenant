using AuthECAPI.Models;
using AuthECAPI.Services.CurrentTenant;
using Azure.Storage.Blobs;

namespace AuthECAPI.Services.Blob
{
    public class BlobServiceFactory : IBlobServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly AppDbContext _dbContext;

        public BlobServiceFactory(
            IServiceProvider serviceProvider,
            ICurrentTenantService currentTenantService,
            AppDbContext dbContext)
        {
            _serviceProvider = serviceProvider;
            _currentTenantService = currentTenantService;
            _dbContext = dbContext;
        }

        public IBlobService GetClient()
        {
            var tenantId = _currentTenantService.TenantId;

            if (string.IsNullOrEmpty(tenantId))
                throw new InvalidOperationException("Tenant ID is missing.");

            // Fix: Convert tenantId to Guid before comparison
            if (!Guid.TryParse(tenantId, out var tenantGuid))
                throw new InvalidOperationException("Invalid Tenant ID format.");

            var tenant = _dbContext.Tenants.FirstOrDefault(t => t.TenantID == tenantGuid);
            if (tenant == null)
                throw new InvalidOperationException($"No tenant found with ID: {tenantId}");

            return tenant.Provider.ToLower() switch
            {
                "azure" => _serviceProvider.GetRequiredService<AzureBlobService>(),
                "s3" => _serviceProvider.GetRequiredService<S3BlobService>(),
                _ => throw new ArgumentException($"Unsupported blob provider: {tenant.Provider}")
            };
        }

        public IBlobStorageService GetClient(string provider)
        {
            return provider?.ToLower() switch
            {
                "azure" => _serviceProvider.GetRequiredService<AzureBlobStorageService>(),
                //"s3" => _serviceProvider.GetRequiredService<S3BlobStorageService>(),
                _ => throw new ArgumentException($"Unsupported blob provider: {provider}")
            };
        }
    }

}
