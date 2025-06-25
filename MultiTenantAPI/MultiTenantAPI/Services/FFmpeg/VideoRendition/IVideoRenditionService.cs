namespace MultiTenantAPI.Services.FFmpeg.VideoRendition
{
    public interface IVideoRenditionService
    {
        public Task<string> GenerateVideoRenditionsAsync(string inputPath, string rendition);

        public Task<bool> GetRenditionLabelAsync(string filePath, string requiredRendition);
    }
}
