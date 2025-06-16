using Microsoft.AspNetCore.Mvc;

namespace AuthECAPI.DTO
{
    public class CreateProductRequest
    {
        public IFormFile File { get; set; }

        [FromForm]
        public bool IsPrivate { get; set; }

    }

}
