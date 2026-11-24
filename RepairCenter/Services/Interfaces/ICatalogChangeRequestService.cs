using RepairCenter.DTOs.CatalogChangeRequests;
using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface ICatalogChangeRequestService
{
    Task<List<CatalogChangeRequest>> GetAllAsync(bool isAdmin, int? userId);
    Task<CatalogChangeRequestResult<CatalogChangeRequest>> CreateAsync(CreateCatalogChangeRequest request, int userId);
    Task<CatalogChangeRequestResult<bool>> CancelAsync(int id, int userId);
    Task<CatalogChangeRequestResult<CatalogChangeRequest>> ApproveAsync(int id, int adminId, ResolveCatalogChangeRequest request);
    Task<CatalogChangeRequestResult<CatalogChangeRequest>> RejectAsync(int id, int adminId, ResolveCatalogChangeRequest request);
}
