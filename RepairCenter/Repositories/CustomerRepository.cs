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

        public async Task<List<Customer>> SearchAsync(string query, int limit)
        {
            var phoneQuery = new string(query.Where(char.IsDigit).ToArray());
            var isPhoneSearch = phoneQuery.Length >= 2 &&
                query.All(character => char.IsDigit(character) || "+()- .".Contains(character));
            var customers = _context.Customers.AsNoTracking().AsQueryable();

            if (isPhoneSearch)
            {
                customers = customers.Where(customer =>
                    customer.Name.Contains(query) ||
                    (customer.Email != null && customer.Email.Contains(query)) ||
                    (customer.PhoneNumber != null &&
                     customer.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "").Contains(phoneQuery)));
            }
            else
            {
                customers = customers.Where(customer =>
                    customer.Name.Contains(query) ||
                    (customer.Email != null && customer.Email.Contains(query)));
            }

            return await customers
                .OrderBy(customer => customer.Name)
                .ThenBy(customer => customer.Id)
                .Take(limit)
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