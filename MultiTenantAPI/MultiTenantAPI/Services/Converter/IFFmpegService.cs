namespace MultiTenantAPI.Services.Converter
{
    public interface IFFmpegService
    {
        public Task<string> ConvertToMp4Async(string inputPath);

        public Task<string> ConvertToMp3Async(string inputPath);

        public Task<bool> ExtractThumbnailAsync(string filePath, string thumbnail);

        public Task<string> GenerateVideoRenditionsAsync(string inputPath, string rendition);

        public Task<bool> GetRenditionLabelAsync(string filePath, string requiredRendition);
    }
}
