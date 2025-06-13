using AuthECAPI.Models;
using AuthECAPI.Services.DTOs;

namespace AuthECAPI.Services.Products
{
    public interface IProductService
    {
        IEnumerable<Product> GetAllProducts();
        Task<Stream> GetProductById(string name);
        Task<Product> CreateProduct(CreateProductRequest request);
        Task<bool> DeleteProduct(string name);
    }
}
