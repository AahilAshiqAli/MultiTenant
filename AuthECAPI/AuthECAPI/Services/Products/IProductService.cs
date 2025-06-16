using AuthECAPI.DTO;
using AuthECAPI.Models;

namespace AuthECAPI.Services.Products
{
    public interface IProductService
    {
        IEnumerable<Product> GetAllProducts();

        IEnumerable<Product> FilterProductsByName(string name);
        Task<Stream> GetProductByName(string name);
        Task<Product> CreateProduct(CreateProductRequest request);
        Task<bool> DeleteProduct(string name);
    }
}
