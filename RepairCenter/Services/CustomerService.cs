using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;

        public CustomerService(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Customer>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<List<Customer>> SearchAsync(string query, int limit)
        {
            return _repository.SearchAsync(query, limit);
        }

        public Task<Customer?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            customer.Name = customer.Name.Trim();
            customer.Email = NormalizeOptional(customer.Email);
            customer.PhoneNumber = NormalizeOptional(customer.PhoneNumber);
            customer.Address = NormalizeOptional(customer.Address);
            await _repository.AddAsync(customer);

            return customer;
        }

        public async Task<Customer?> UpdateAsync(int id, Customer customer)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return null;

            existing.Name = customer.Name.Trim();
            existing.Email = NormalizeOptional(customer.Email);
            existing.PhoneNumber = NormalizeOptional(customer.PhoneNumber);
            existing.Address = NormalizeOptional(customer.Address);
            await _repository.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer is null)
                return false;

            await _repository.DeleteAsync(customer);
            return true;
        }

        private static string? NormalizeOptional(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
