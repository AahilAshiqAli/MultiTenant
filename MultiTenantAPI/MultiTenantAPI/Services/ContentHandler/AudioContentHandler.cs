using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.FFmpeg.Converter;

namespace MultiTenantAPI.Services.ContentHandler
{
    public class AudioContentHandler : IContentHandler
    {
        private readonly IConverterService _converterService;

        public AudioContentHandler(IConverterService converterService)
        {
            _converterService = converterService;
        }

        public bool CanHandle(string extension) =>
            new[] { ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma" }.Contains(extension);

        public async Task<ContentProcessingResult> ProcessAsync(ContentMessage message, string tempFilePath)
        {
            var processedFile = await _converterService.ConvertToMp3Async(tempFilePath);

            if (string.IsNullOrEmpty(processedFile))
                throw new Exception("Failed to convert audio file.");

            return new ContentProcessingResult
            {
                ProcessedFilePath = processedFile,
                FinalFileName = Path.ChangeExtension(message.uniqueFileName, ".mp3"),
                ThumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "music.jpg")
            };
        }
    }
}
