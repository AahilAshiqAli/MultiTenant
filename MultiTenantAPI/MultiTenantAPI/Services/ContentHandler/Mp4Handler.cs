using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;

namespace MultiTenantAPI.Services.ContentHandler
{
    public class Mp4Handler : IContentHandler
    {
        private readonly IVideoRenditionService _renditionService;
        private readonly IThumbnailService _thumbnailService;

        public Mp4Handler(IVideoRenditionService renditionService, IThumbnailService thumbnailService)
        {
            _renditionService = renditionService;
            _thumbnailService = thumbnailService;
        }

        public bool CanHandle(string extension) => extension == ".mp4";

        public async Task<ContentProcessingResult> ProcessAsync(ContentMessage message, string tempFilePath)
        {
            string finalFileName = Path.ChangeExtension(message.uniqueFileName, ".mp4");

            string thumbnail = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads",
                Path.ChangeExtension(finalFileName, ".jpg")
            );

            bool thumbSuccess = await _thumbnailService.ExtractThumbnailAsync(tempFilePath, thumbnail);
            if (!thumbSuccess)
                throw new Exception("Failed to extract thumbnail for MP4.");

            var valid = await _renditionService.GetRenditionLabelAsync(tempFilePath, message.RequiredRendition);
            if (!valid)
                throw new Exception("Video doesn't meet rendition requirements.");

            var renditionedFile = await _renditionService.GenerateVideoRenditionsAsync(tempFilePath, message.RequiredRendition);
            finalFileName = AddRenditionToFileName(finalFileName, message.RequiredRendition);

            return new ContentProcessingResult
            {
                ProcessedFilePath = renditionedFile,
                FinalFileName = finalFileName,
                ThumbnailPath = thumbnail
            };
        }

        private string AddRenditionToFileName(string name, string rendition)
        {
            var withoutExt = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name);
            return $"{withoutExt}{rendition}{ext}";
        }
    }

}
