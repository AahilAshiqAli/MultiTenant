using Amazon.S3;
using AuthECAPI.Services.Blob;
using Azure.Storage.Blobs;

namespace AuthECAPI.Extensions
{

    public static class BlobStorageExtensions
    {
        public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IBlobServiceFactory, BlobServiceFactory>();
            

            services.AddSingleton(x =>
            {
                string? connectionString = configuration.GetConnectionString("AzureBlobStorageConnectionString");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("AzureBlobStorageConnectionString is not configured.");

                return new BlobServiceClient(connectionString);
            });

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
                return new AmazonS3Client("AWS_ACCESS_KEY", "AWS_SECRET_KEY", config);
            });

            // Register concrete types, not as IBlobService
            services.AddScoped<AzureBlobService>();
            services.AddScoped<S3BlobService>();
            services.AddScoped<AzureBlobStorageService>();


            return services;
        }
    }
}
