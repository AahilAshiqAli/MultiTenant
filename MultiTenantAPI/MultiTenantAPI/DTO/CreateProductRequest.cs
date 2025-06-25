using Microsoft.AspNetCore.Mvc;

namespace MultiTenantAPI.DTO
{
    public class CreateProductRequest
    {
        public string OriginalFileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string BlobFileName { get; set; }

        [FromForm]
        public bool IsPrivate { get; set; }

        [FromForm]
        public string Rendition { get; set; }

    }

}
