﻿using MultiTenantAPI.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MultiTenantAPI.Services.Blob       
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(AppDbContext appDbContext, IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            string? connectionString = configuration.GetConnectionString("AzureBlobStorageConnectionString");
            _blobServiceClient = new BlobServiceClient(connectionString);
            _dbContext = appDbContext;
            _logger = logger;

            _logger.LogInformation("AzureBlobStorageService initialized with connection string: {ConnectionString}", connectionString);
        }

        public async Task<string> CreateTenantContainerAsync(string container, Tenant settings)
        {
            _logger.LogInformation("Starting CreateTenantContainerAsync");

            if (_blobServiceClient == null)
            {
                _logger.LogError("BlobServiceClient is not initialized.");
                throw new InvalidOperationException("Azure BlobServiceClient is not initialized. Check your connection string configuration.");
            }

            if (settings.RetentionDays <= 0)
            {
                _logger.LogError("Invalid RetentionDays: {RetentionDays} for tenantId: {TenantId}", settings.RetentionDays, container);
                throw new ArgumentException("RetentionDays must be a positive integer.", nameof(settings.RetentionDays));
            }

            var containerName = container;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync();
                _logger.LogInformation("Container {ContainerName} created or already exists.", containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create container {ContainerName}", containerName);
                throw;
            }

            BlobServiceProperties properties;
            try
            {
                var propsResponse = await _blobServiceClient.GetPropertiesAsync();
                if (propsResponse.Value == null)
                {
                    _logger.LogError("Failed to retrieve existing Blob service properties");
                    throw new InvalidOperationException("Failed to retrieve existing Blob service properties.");
                }
                properties = propsResponse.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Blob service properties");
                throw;
            }

            // Check and update retention only if current is less than desired
            if (properties.DeleteRetentionPolicy == null ||
                !properties.DeleteRetentionPolicy.Enabled ||
                properties.DeleteRetentionPolicy.Days < settings.RetentionDays)
            {
                properties.DeleteRetentionPolicy = new BlobRetentionPolicy
                {
                    Enabled = true,
                    Days = settings.RetentionDays
                };

                try
                {
                    await _blobServiceClient.SetPropertiesAsync(properties);
                    _logger.LogInformation("Retention policy updated to {Days} days", settings.RetentionDays);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update Blob service properties");
                    throw;
                }
            }


            try
            {
                var containerProperties = await containerClient.GetPropertiesAsync();
                if (settings.EnableVersioning)
                    containerProperties.Value.Metadata["versioning"] = "enabled";
                containerProperties.Value.Metadata["AccessTier"] = settings.DefaultBlobTier;
                await containerClient.SetMetadataAsync(containerProperties.Value.Metadata);
                _logger.LogInformation("Enabled versioning for container {ContainerName}", containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable versioning for container {ContainerName}", containerName);
                throw;
            }

            



            _logger.LogInformation("CreateTenantContainerAsync completed");
            return containerName;
        }

        public async Task<IEnumerable<string>> ListAllContainersAsync()
        {
            _logger.LogInformation("Listing all containers from database.");
            try
            {
                var containers = await _dbContext.Tenants
                    .IgnoreQueryFilters()
                    .Select(t => t.Container)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} containers from database.", containers.Count);
                return containers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list all containers from database.");
                throw;
            }
        }

        public async Task<bool> DeleteContainerAsync(string tenantId)
        {
            _logger.LogInformation("Starting DeleteContainerAsync for tenantId: {TenantId}", tenantId);
            var containerName = $"tenant-{tenantId.ToLower()}";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            try
            {
                await containerClient.DeleteIfExistsAsync();
                _logger.LogInformation("Container {ContainerName} deleted successfully for tenantId: {TenantId}", containerName, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete container {ContainerName} for tenantId: {TenantId}", containerName, tenantId);
                return false;
            }
        }
    }

}
