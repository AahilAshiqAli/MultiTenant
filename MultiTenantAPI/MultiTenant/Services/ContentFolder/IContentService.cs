using AuthECAPI.DTO;
using AuthECAPI.Models;
using Microsoft.AspNetCore.Mvc;


namespace AuthECAPI.Services.ContentFolder
{
    public interface IContentService
    {
        IEnumerable<ContentDto> GetAllContent();

        IEnumerable<ContentDto> FilterContentByName(string name);

        Task<IActionResult> StreamVideoAsync(int id, HttpRequest request, HttpResponse response);

        public Task ProcessUploadedContentAsync(ContentMessage message);

        public Task CreateContent(CreateProductRequest request);

        Task<bool> DeleteContent(string name);
    }
}
