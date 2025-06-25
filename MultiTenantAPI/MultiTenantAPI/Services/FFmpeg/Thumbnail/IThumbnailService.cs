namespace MultiTenantAPI.Services.FFmpeg.Thumbnail
{
    public interface IThumbnailService
    {
        public Task<bool> ExtractThumbnailAsync(string filePath, string thumbnail);
    }
}
