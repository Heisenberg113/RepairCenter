namespace RepairCenter.Repositories.Interfaces
{
    using RepairCenter.Entities;

    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();

        Task<List<Customer>> SearchAsync(string query, int limit);

        Task<Customer?> GetByIdAsync(int id);

        Task AddAsync(Customer customer);

        Task UpdateAsync(Customer customer);

        Task DeleteAsync(Customer customer);
    }
}