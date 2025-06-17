using Azure;
using Azure.Storage.Blobs.Models;

namespace AuthECAPI.Services.Blob
{
    public interface IBlobService
    {
        Task<string> UploadAsync(Stream stream, string fileName, string contentType);
        Task<Stream> GetBlobAsync(string fileName, long? start = null, long? end = null);
        Task<IEnumerable<string>> ListBlobsAsync();
        Task<bool> DeleteBlobAsync(string fileName);
    
    }

}
