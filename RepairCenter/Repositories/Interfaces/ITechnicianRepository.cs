using RepairCenter.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace RepairCenter.Repositories.Interfaces;

public interface ITechnicianRepository
{
    Task<List<Technician>> GetAllAsync();
    Task<Technician?> GetByIdAsync(int id);
    Task<List<DeviceType>> GetAllDeviceTypesAsync();
    Task<List<DeviceType>> GetDeviceTypesByIdsAsync(IReadOnlyCollection<int> ids);
    Task<bool> UserAvailableAsync(int? userId, int? exceptTechnicianId = null);
    Task<bool> UserHasTechnicianRoleAsync(int userId);
    Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? exceptTechnicianId = null);
    Task<bool> HasRepairTicketsAsync(int technicianId);
    Task<IDbContextTransaction> BeginSerializableTransactionAsync();
    Task AddAsync(Technician technician);
    Task UpdateAsync(Technician technician);
    Task DeleteAsync(Technician technician);
}
