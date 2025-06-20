using Azure.Storage.Blobs;

namespace AuthECAPI.Services.Blob
{
    public interface IBlobServiceFactory
    {
        IBlobService GetClient();

        IBlobStorageService GetClient(string provider);

    }
}
