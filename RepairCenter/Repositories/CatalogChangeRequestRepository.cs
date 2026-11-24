using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public sealed class CatalogChangeRequestRepository(RepairCenterDbContext db) : ICatalogChangeRequestRepository
{
    public async Task<List<CatalogChangeRequest>> GetAllAsync(bool isAdmin, int? userId)
    {
        var query = db.CatalogChangeRequests.AsNoTracking().Include(x => x.RequestedByUser).AsQueryable();
        query = isAdmin
            ? query.Where(x => x.Status == "Pending")
            : query.Where(x => x.RequestedByUserId == userId);
        return await query.OrderByDescending(x => x.RequestedAt).Take(200).ToListAsync();
    }

    public Task<bool> DeviceTypeExistsAsync(int id) => db.DeviceTypes.AnyAsync(x => x.Id == id);

    public Task<bool> TargetExistsAsync(string itemType, int id) => itemType == "RepairService"
        ? db.RepairServices.AnyAsync(x => x.Id == id)
        : db.SpareParts.AnyAsync(x => x.Id == id);

    public Task<bool> RepairServiceNameExistsAsync(string name, int? exceptId)
    {
        var query = db.RepairServices.Where(x => x.Name == name);
        if (exceptId.HasValue) query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync();
    }

    public Task<bool> SparePartNumberExistsAsync(string partNumber, int? exceptId)
    {
        var query = db.SpareParts.Where(x => x.PartNumber == partNumber);
        if (exceptId.HasValue) query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync();
    }

    public async Task AddAsync(CatalogChangeRequest request)
    {
        db.CatalogChangeRequests.Add(request);
        await db.SaveChangesAsync();
        await db.Entry(request).Reference(x => x.RequestedByUser).LoadAsync();
    }

    public Task<CatalogChangeRequest?> GetOwnedAsync(int id, int userId) =>
        db.CatalogChangeRequests.FirstOrDefaultAsync(x => x.Id == id && x.RequestedByUserId == userId);

    public Task<CatalogChangeRequest?> GetByIdAsync(int id) =>
        db.CatalogChangeRequests.FirstOrDefaultAsync(x => x.Id == id);

    public Task<IDbContextTransaction> BeginTransactionAsync() => db.Database.BeginTransactionAsync();

    public Task<RepairService?> GetRepairServiceAsync(int id) => db.RepairServices.FirstOrDefaultAsync(x => x.Id == id);

    public Task<SparePart?> GetSparePartAsync(int id) => db.SpareParts.FirstOrDefaultAsync(x => x.Id == id);

    public void Add(RepairService service) => db.RepairServices.Add(service);

    public void Add(SparePart part) => db.SpareParts.Add(part);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
