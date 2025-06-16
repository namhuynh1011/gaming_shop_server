using gaming_shop_api.Models;

namespace gaming_shop_server.Repositories
{
    public interface IBrandRepository
    {
        Task<IEnumerable<Brand>> GetAllAsync();
        Task<Brand?> GetByIdAsync(int id);
        Task<Brand> AddAsync(Brand brand);
        Task<Brand> UpdateAsync(int id, Brand brand);
        Task<bool> DeleteAsync(int id);
    }
}
