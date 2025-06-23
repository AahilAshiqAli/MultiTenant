using Microsoft.AspNetCore.Mvc;

namespace MultiTenantAPI.DTO
{
    public class CreateProductRequest
    {
        public IFormFile File { get; set; }

        [FromForm]
        public bool IsPrivate { get; set; }

        [FromForm]
        public string rendition { get; set; }

    }

}
