using gaming_shop_api.Models;

namespace gaming_shop_server.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product> UpdateAsync(int id, Product product);
        Task<bool> DeleteAsync(int id);
    }
}
