using Azure.Storage.Blobs;

namespace MultiTenantAPI.Services.Blob
{
    public interface IBlobServiceFactory
    {
        IBlobService GetClient();

        IBlobStorageService GetClient(string provider);

    }
}
