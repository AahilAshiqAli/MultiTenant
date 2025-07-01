using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.FFmpeg.Converter;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;

namespace MultiTenantAPI.Services.ContentHandler
{
    public class VideoContentHandler : IContentHandler
    {
        private readonly IConverterService _converterService;
        private readonly IVideoRenditionService _renditionService;
        private readonly IThumbnailService _thumbnailService;

        public VideoContentHandler(
            IConverterService converterService,
            IVideoRenditionService renditionService,
            IThumbnailService thumbnailService)
        {
            _converterService = converterService;
            _renditionService = renditionService;
            _thumbnailService = thumbnailService;
        }

        public bool CanHandle(string extension) =>
            new[] { ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" }.Contains(extension);

        public async Task<ContentProcessingResult> ProcessAsync(ContentMessage message, string tempFilePath)
        {
            var processedFile = await _converterService.ConvertToMp4Async(tempFilePath);

            if (string.IsNullOrEmpty(processedFile))
                throw new Exception("Failed to convert video file.");

            var valid = await _renditionService.GetRenditionLabelAsync(processedFile, message.RequiredRendition);
            if (!valid)
                throw new Exception("Video doesn't meet rendition requirements.");

            string thumbnail = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads",
                Path.ChangeExtension(message.uniqueFileName, ".jpg"));

            var thumbSuccess = await _thumbnailService.ExtractThumbnailAsync(processedFile, thumbnail);
            if (!thumbSuccess || !File.Exists(thumbnail))
                throw new Exception("Thumbnail generation failed.");

            var renditionedFile = await _renditionService.GenerateVideoRenditionsAsync(processedFile, message.RequiredRendition);

            return new ContentProcessingResult
            {
                ProcessedFilePath = renditionedFile,
                FinalFileName = AddRenditionToFileName(Path.ChangeExtension(message.uniqueFileName, ".mp4"), message.RequiredRendition),
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
