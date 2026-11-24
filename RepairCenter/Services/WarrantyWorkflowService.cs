using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Contracts;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class WarrantyWorkflowService(IWarrantyWorkflowRepository repository) : IWarrantyWorkflowService
{
    public Task<Warranty?> GetWarrantyAsync(int warrantyId) => repository.GetWarrantyAsync(warrantyId);
    public Task<List<WarrantyClaim>> GetClaimsAsync(int warrantyId) => repository.GetClaimsAsync(warrantyId);
    public Task<List<WarrantyClaim>> GetPendingClaimsAsync() => repository.GetPendingClaimsAsync();
    public Task<List<WarrantyClaim>> GetClaimsForTechnicianAsync(int userId) => repository.GetClaimsForTechnicianAsync(userId);
    public Task<WarrantyClaim?> GetClaimAsync(int claimId) => repository.GetClaimAsync(claimId);

    public async Task<(WarrantyClaim? Value, WarrantyWorkflowError Error)> SubmitClaimAsync(
        int warrantyId, int repairTicketDetailId, int actorId, string description)
    {
        description = description?.Trim() ?? string.Empty;
        if (description.Length is < 5 or > 2000) return (null, WarrantyWorkflowError.InvalidRequest);
        var claim = new WarrantyClaim
        {
            WarrantyId = warrantyId,
            RepairTicketDetailId = repairTicketDetailId,
            RequestedByUserId = actorId,
            Description = description
        };
        var error = await repository.SubmitClaimAsync(claim);
        if (error != WarrantyWorkflowError.None) return (null, error);
        var claims = await repository.GetClaimsAsync(warrantyId);
        return (claims.First(x => x.Id == claim.Id), error);
    }

    public Task<WarrantyWorkflowError> AcceptClaimAsync(int claimId, int userId) => repository.AcceptClaimAsync(claimId, userId);

    public Task<WarrantyWorkflowError> AssignClaimAsync(int claimId, int? technicianId) => repository.AssignClaimAsync(claimId, technicianId);

    public async Task<(WarrantyClaim? Value, WarrantyWorkflowError Error)> ResolveClaimAsync(
        int claimId, int actorId, WarrantyClaimStatus status, string note, bool isAdmin)
    {
        note = note?.Trim() ?? string.Empty;
        if (note.Length is < 3 or > 1000) return (null, WarrantyWorkflowError.InvalidRequest);
        var error = await repository.ResolveClaimAsync(claimId, actorId, status, note, isAdmin);
        if (error != WarrantyWorkflowError.None) return (null, error);
        return (await repository.GetClaimAsync(claimId), error);
    }

    public Task<WarrantyWorkflowError> CancelClaimAsync(int claimId, int actorId, bool canManage) =>
        repository.CancelClaimAsync(claimId, actorId, canManage);

    public Task<WarrantyWorkflowError> VoidWarrantyAsync(int warrantyId, int actorId, string reason) =>
        string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Trim().Length > 1000
            ? Task.FromResult(WarrantyWorkflowError.InvalidRequest)
            : repository.VoidWarrantyAsync(warrantyId, actorId, reason.Trim());

    public Task<int> MarkExpiredWarrantiesAsync(DateTime today) => repository.MarkExpiredWarrantiesAsync(today);
}
