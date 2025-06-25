using MultiTenantAPI.DTO;

namespace MultiTenantAPI.Services.ContentProcessor
{
    public interface IContentProcessorService
    {
        public Task ProcessUploadedContentAsync(ContentMessage message);
    }
}
