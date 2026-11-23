using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
namespace RepairCenter.Services.Interfaces;
public interface IWarrantyWorkflowService
{
    Task<Warranty?> GetWarrantyAsync(int warrantyId);
    Task<List<WarrantyClaim>> GetClaimsAsync(int warrantyId);
    Task<List<WarrantyClaim>> GetPendingClaimsAsync();
    Task<List<WarrantyClaim>> GetClaimsForTechnicianAsync(int userId);
    Task<WarrantyClaim?> GetClaimAsync(int claimId);
    Task<(WarrantyClaim? Value, WarrantyWorkflowError Error)> SubmitClaimAsync(int warrantyId, int repairTicketDetailId, int actorId, string description);
    Task<WarrantyWorkflowError> AcceptClaimAsync(int claimId, int userId);
    Task<WarrantyWorkflowError> AssignClaimAsync(int claimId, int? technicianId);
    Task<(WarrantyClaim? Value, WarrantyWorkflowError Error)> ResolveClaimAsync(int claimId, int actorId, WarrantyClaimStatus status, string note, bool isAdmin);
    Task<WarrantyWorkflowError> CancelClaimAsync(int claimId, int actorId, bool canManage);
    Task<WarrantyWorkflowError> VoidWarrantyAsync(int warrantyId, int actorId, string reason);
    Task<int> MarkExpiredWarrantiesAsync(DateTime today);
}
