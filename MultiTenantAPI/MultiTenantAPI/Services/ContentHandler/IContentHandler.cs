using MultiTenantAPI.DTO;

namespace MultiTenantAPI.Services.ContentHandler
{
    public interface IContentHandler
    {
        bool CanHandle(string extension);
        Task<ContentProcessingResult> ProcessAsync(ContentMessage message, string tempFilePath);
    }
}
