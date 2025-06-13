using Amazon.S3;
using Amazon.S3.Model;

namespace AuthECAPI.Services.Blob
{
    public class S3BlobService : IBlobService
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "videos";

        public S3BlobService(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = contentType
            };
            await _s3Client.PutObjectAsync(putRequest);
            return $"https://{BucketName}.s3.amazonaws.com/{fileName}";
        }

        public async Task<Stream> GetBlobAsync(string fileName)
        {
            var response = await _s3Client.GetObjectAsync(BucketName, fileName);
            return response.ResponseStream;
        }

        public async Task<IEnumerable<string>> ListBlobsAsync()
        {
            var request = new ListObjectsV2Request { BucketName = BucketName };
            var response = await _s3Client.ListObjectsV2Async(request);
            return response.S3Objects.Select(o => o.Key);
        }

        public async Task<bool> DeleteBlobAsync(string fileName)
        {
            try
            {
                var response = await _s3Client.DeleteObjectAsync(BucketName, fileName);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                       response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Blob does not exist
                return false;
            }
        }
    }
}
