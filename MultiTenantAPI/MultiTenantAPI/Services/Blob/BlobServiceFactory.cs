using MultiTenantAPI.Models;
using MultiTenantAPI.Services.CurrentTenant;
using Azure.Storage.Blobs;

namespace MultiTenantAPI.Services.Blob
{
    public class BlobServiceFactory : IBlobServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<BlobServiceFactory> _logger;

        public BlobServiceFactory(
            IServiceProvider serviceProvider,
            ICurrentTenantService currentTenantService,
            AppDbContext dbContext,
            ILogger<BlobServiceFactory> logger)
        {   
            _serviceProvider = serviceProvider;
            _currentTenantService = currentTenantService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public IBlobService GetClient()
        {
            var tenantId = _currentTenantService.TenantId;
            _logger.LogInformation("Attempting to get blob client for tenantId: {TenantId}", tenantId);

            if (tenantId.HasValue)
            {
                _logger.LogError("Tenant ID is missing.");
                throw new InvalidOperationException("Tenant ID is missing.");
            }

            var tenant = _dbContext.Tenants.FirstOrDefault(t => t.TenantID == tenantId);
            if (tenant == null)
            {
                _logger.LogError("No tenant found with ID: {TenantId}", tenantId);
                throw new InvalidOperationException($"No tenant found with ID: {tenantId}");
            }

            _logger.LogInformation("Tenant found: {TenantName}, Provider: {Provider}", tenant.Name, tenant.Provider);

            var provider = tenant.Provider.ToLower();
            switch (provider)
            {
                case "azure":
                    _logger.LogInformation("Returning AzureBlobService for tenant {TenantId}", tenantId);
                    return _serviceProvider.GetRequiredService<AzureBlobService>();
                case "s3":
                    _logger.LogInformation("Returning S3BlobService for tenant {TenantId}", tenantId);
                    return _serviceProvider.GetRequiredService<S3BlobService>();
                default:
                    _logger.LogError("Unsupported blob provider: {Provider}", tenant.Provider);
                    throw new ArgumentException($"Unsupported blob provider: {tenant.Provider}");
            }
        }

        public IBlobStorageService GetClient(string provider)
        {
            _logger.LogInformation("Attempting to get blob storage client for provider: {Provider}", provider);

            switch (provider?.ToLower())
            {
                case "azure":
                    _logger.LogInformation("Returning AzureBlobStorageService for provider: {Provider}", provider);
                    return _serviceProvider.GetRequiredService<AzureBlobStorageService>();
                //case "s3":
                //    _logger.LogInformation("Returning S3BlobStorageService for provider: {Provider}", provider);
                //    return _serviceProvider.GetRequiredService<S3BlobStorageService>();
                default:
                    _logger.LogError("Unsupported blob provider: {Provider}", provider);
                    throw new ArgumentException($"Unsupported blob provider: {provider}");
            }
        }
    }

}
