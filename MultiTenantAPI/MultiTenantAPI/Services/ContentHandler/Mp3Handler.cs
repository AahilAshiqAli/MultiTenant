using MultiTenantAPI.DTO;

namespace MultiTenantAPI.Services.ContentHandler
{
    public class Mp3Handler : IContentHandler
    {
        public bool CanHandle(string extension) => extension == ".mp3";

        public Task<ContentProcessingResult> ProcessAsync(ContentMessage message, string tempFilePath)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            string thumbnail = Path.Combine(uploadsFolder, "music.jpg");

            return Task.FromResult(new ContentProcessingResult
            {
                ProcessedFilePath = tempFilePath,
                FinalFileName = Path.ChangeExtension(message.uniqueFileName, ".mp3"),
                ThumbnailPath = thumbnail
            });
        }
    }
}
