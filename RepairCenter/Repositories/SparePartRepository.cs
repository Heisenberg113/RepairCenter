using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class SparePartRepository(RepairCenterDbContext context) : ISparePartRepository
{
    public async Task<List<SparePart>> GetAllAsync(string? search, bool activeOnly, bool lowStockOnly)
    {
        var query = context.SpareParts.AsNoTracking().Include(part => part.DeviceType).AsQueryable();
        if (activeOnly)
            query = query.Where(part => part.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(part => part.PartNumber.Contains(term) || part.Name.Contains(term));
        }
        if (lowStockOnly)
            query = query.Where(part => part.QuantityInStock <= part.ReorderLevel);

        return await query.OrderBy(part => part.Name).ToListAsync();
    }

    public Task<SparePart?> GetByIdAsync(int id) => context.SpareParts
        .Include(part => part.DeviceType)
        .FirstOrDefaultAsync(part => part.Id == id);

    public Task<bool> DeviceTypeExistsAsync(int id) => context.DeviceTypes.AnyAsync(x => x.Id == id);

    public Task<bool> PartNumberExistsAsync(string partNumber, int? exceptId = null)
    {
        var query = context.SpareParts.Where(part => part.PartNumber == partNumber);
        if (exceptId.HasValue)
            query = query.Where(part => part.Id != exceptId.Value);
        return query.AnyAsync();
    }

    public async Task AddAsync(SparePart sparePart)
    {
        await context.SpareParts.AddAsync(sparePart);
        await context.SaveChangesAsync();
    }

    public Task UpdateAsync(SparePart sparePart) => context.SaveChangesAsync();
}
