using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<List<Customer>> SearchAsync(string query, int limit);
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> CreateAsync(Customer customer);
        Task<Customer?> UpdateAsync(int id, Customer customer);
        Task<bool> DeleteAsync(int id);
    }
}
