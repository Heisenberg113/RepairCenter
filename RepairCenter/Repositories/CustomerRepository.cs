namespace RepairCenter.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using RepairCenter.Data;
    using RepairCenter.Entities;
    using RepairCenter.Repositories.Interfaces;

    public class CustomerRepository : ICustomerRepository
    {
        private readonly RepairCenterDbContext _context;

        public CustomerRepository(RepairCenterDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Customer customer)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }
}