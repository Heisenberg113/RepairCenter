using Microsoft.EntityFrameworkCore.Storage;
using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface ICatalogChangeRequestRepository
{
    Task<List<CatalogChangeRequest>> GetAllAsync(bool isAdmin, int? userId);
    Task<bool> DeviceTypeExistsAsync(int id);
    Task<bool> TargetExistsAsync(string itemType, int id);
    Task<bool> RepairServiceNameExistsAsync(string name, int? exceptId);
    Task<bool> SparePartNumberExistsAsync(string partNumber, int? exceptId);
    Task AddAsync(CatalogChangeRequest request);
    Task<CatalogChangeRequest?> GetOwnedAsync(int id, int userId);
    Task<CatalogChangeRequest?> GetByIdAsync(int id);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<RepairService?> GetRepairServiceAsync(int id);
    Task<SparePart?> GetSparePartAsync(int id);
    void Add(RepairService service);
    void Add(SparePart part);
    Task SaveChangesAsync();
}
