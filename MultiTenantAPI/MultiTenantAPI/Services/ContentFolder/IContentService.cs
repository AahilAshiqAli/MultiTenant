using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using Microsoft.AspNetCore.Mvc;


namespace MultiTenantAPI.Services.ContentFolder
{
    public interface IContentService
    {
        IEnumerable<ContentDto> GetAllContent();

        IEnumerable<ContentDto> FilterContentByName(string name);

        Task<string> StreamVideoAsync(int id);

        public Task ProcessUploadedContentAsync(ContentMessage message);

        public Task CreateContent(CreateProductRequest request);

        Task<bool> DeleteContent(string name);

        Task<bool> DeleteUserContentAsync(string userId);

    }
}
