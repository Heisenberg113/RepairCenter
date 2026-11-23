using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class WarrantyWorkflowRepository(RepairCenterDbContext db) : IWarrantyWorkflowRepository
{
    public Task<Warranty?> GetWarrantyAsync(int warrantyId) => db.Warranties.AsNoTracking()
        .Include(x => x.RepairTicket).ThenInclude(x => x.Details)
        .Include(x => x.RepairTicket).ThenInclude(x => x.Technician)
        .Include(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .FirstOrDefaultAsync(x => x.Id == warrantyId);

    public Task<List<WarrantyClaim>> GetClaimsAsync(int warrantyId) => db.WarrantyClaims.AsNoTracking()
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
        .Include(x => x.RepairTicketDetail)
        .Include(x => x.GeneratedRepairTicket)
        .Include(x => x.RequestedByUser).Include(x => x.ResolvedByUser)
        .Include(x => x.AssignedTechnician).ThenInclude(x => x!.User)
        .Where(x => x.WarrantyId == warrantyId).OrderByDescending(x => x.SubmittedAt).ThenByDescending(x => x.Id).ToListAsync();

    public Task<List<WarrantyClaim>> GetPendingClaimsAsync() => db.WarrantyClaims.AsNoTracking()
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
        .Include(x => x.RepairTicketDetail)
        .Include(x => x.RequestedByUser)
        .Include(x => x.AssignedTechnician).ThenInclude(x => x!.User)
        .Where(x => x.Status == WarrantyClaimStatus.Pending || x.Status == WarrantyClaimStatus.Assigned)
        .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id).ToListAsync();

    public async Task<List<WarrantyClaim>> GetClaimsForTechnicianAsync(int userId)
    {
        var technician = await db.Technicians.AsNoTracking().Include(x => x.DeviceTypes)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (technician is null) return [];
        var deviceTypeIds = technician.DeviceTypes.Select(x => x.DeviceTypeId).ToArray();
        return await db.WarrantyClaims.AsNoTracking()
            .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Customer)
            .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
            .Include(x => x.RepairTicketDetail).Include(x => x.RequestedByUser)
            .Include(x => x.AssignedTechnician).ThenInclude(x => x!.User)
            .Where(x =>
                (x.Status == WarrantyClaimStatus.Assigned && x.AssignedTechnicianId == technician.Id) ||
                (x.Status == WarrantyClaimStatus.Pending && x.AssignedTechnicianId == null &&
                 x.RequestedByUserId != userId && deviceTypeIds.Contains(x.Warranty.RepairTicket.Device.DeviceTypeId)))
            .OrderBy(x => x.Status == WarrantyClaimStatus.Assigned ? 0 : 1)
            .ThenBy(x => x.SubmittedAt).ThenBy(x => x.Id).ToListAsync();
    }

    public Task<WarrantyClaim?> GetClaimAsync(int claimId) => db.WarrantyClaims.AsNoTracking()
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
        .Include(x => x.RepairTicketDetail)
        .Include(x => x.GeneratedRepairTicket)
        .Include(x => x.RequestedByUser).Include(x => x.ResolvedByUser)
        .Include(x => x.AssignedTechnician).ThenInclude(x => x!.User)
        .FirstOrDefaultAsync(x => x.Id == claimId);

    public async Task<WarrantyWorkflowError> SubmitClaimAsync(WarrantyClaim claim)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(claim.RequestedByUserId)) return await Rollback(tx, WarrantyWorkflowError.InvalidActor);
        var warranty = await db.Warranties.FirstOrDefaultAsync(x => x.Id == claim.WarrantyId);
        if (warranty is null) return await Rollback(tx, WarrantyWorkflowError.WarrantyNotFound);
        if (warranty.Status == WarrantyStatus.Active && warranty.EndDate.Date < DateTime.UtcNow.Date)
        {
            warranty.Status = WarrantyStatus.Expired;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return WarrantyWorkflowError.WarrantyExpired;
        }
        if (warranty.Status == WarrantyStatus.Expired) return await Rollback(tx, WarrantyWorkflowError.WarrantyExpired);
        if (warranty.Status != WarrantyStatus.Active) return await Rollback(tx, WarrantyWorkflowError.WarrantyNotActive);
        var detail = claim.RepairTicketDetailId.HasValue
            ? await db.RepairTicketDetails.FirstOrDefaultAsync(x => x.Id == claim.RepairTicketDetailId && x.RepairTicketId == warranty.RepairTicketId)
            : null;
        if (detail is null) return await Rollback(tx, WarrantyWorkflowError.ItemNotFound);
        if (!detail.WarrantyStartDate.HasValue || !detail.WarrantyEndDate.HasValue || detail.WarrantyDuration <= 0)
            return await Rollback(tx, WarrantyWorkflowError.ItemNotCovered);
        if (detail.WarrantyStartDate.Value.Date > claim.SubmittedAt.Date || detail.WarrantyEndDate.Value.Date < claim.SubmittedAt.Date)
            return await Rollback(tx, WarrantyWorkflowError.ItemWarrantyExpired);
        if (await db.WarrantyClaims.AnyAsync(x => x.WarrantyId == claim.WarrantyId &&
                x.RepairTicketDetailId == detail.Id &&
                (x.Status == WarrantyClaimStatus.Pending || x.Status == WarrantyClaimStatus.Assigned)))
            return await Rollback(tx, WarrantyWorkflowError.PendingClaimExists);
        await db.WarrantyClaims.AddAsync(claim);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public async Task<WarrantyWorkflowError> AcceptClaimAsync(int claimId, int userId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(userId)) return await Rollback(tx, WarrantyWorkflowError.InvalidActor);
        var technician = await db.Technicians.Include(x => x.DeviceTypes)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (technician is null) return await Rollback(tx, WarrantyWorkflowError.TechnicianNotQualified);
        var claim = await db.WarrantyClaims.Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
            .Include(x => x.RepairTicketDetail).FirstOrDefaultAsync(x => x.Id == claimId);
        if (claim is null) return await Rollback(tx, WarrantyWorkflowError.ClaimNotFound);
        if (claim.Status != WarrantyClaimStatus.Pending) return await Rollback(tx, WarrantyWorkflowError.ClaimNotPending);
        if (claim.AssignedTechnicianId.HasValue) return await Rollback(tx, WarrantyWorkflowError.ClaimAlreadyAssigned);
        if (claim.RequestedByUserId == userId) return await Rollback(tx, WarrantyWorkflowError.NotClaimOwner);
        if (!IsQualified(technician, claim.Warranty.RepairTicket.Device.DeviceTypeId))
            return await Rollback(tx, WarrantyWorkflowError.TechnicianNotQualified);
        if (!IsCoveredAtSubmission(claim)) return await Rollback(tx, WarrantyWorkflowError.ItemWarrantyExpired);
        claim.AssignedTechnicianId = technician.Id;
        claim.AssignedAt = DateTime.UtcNow;
        claim.Status = WarrantyClaimStatus.Assigned;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public async Task<WarrantyWorkflowError> AssignClaimAsync(int claimId, int? technicianId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var claim = await db.WarrantyClaims.Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
            .FirstOrDefaultAsync(x => x.Id == claimId);
        if (claim is null) return await Rollback(tx, WarrantyWorkflowError.ClaimNotFound);
        if (claim.Status is not (WarrantyClaimStatus.Pending or WarrantyClaimStatus.Assigned))
            return await Rollback(tx, WarrantyWorkflowError.ClaimNotPending);
        if (technicianId is null)
        {
            claim.AssignedTechnicianId = null;
            claim.AssignedAt = null;
            claim.Status = WarrantyClaimStatus.Pending;
        }
        else
        {
            var technician = await db.Technicians.Include(x => x.DeviceTypes)
                .FirstOrDefaultAsync(x => x.Id == technicianId && x.IsActive && x.UserId != claim.RequestedByUserId);
            if (technician is null || !technician.UserId.HasValue ||
                !await IsActiveUserAsync(technician.UserId.Value) ||
                !await db.UserRoles.AnyAsync(x => x.UserId == technician.UserId.Value && x.Role.IsActive && x.Role.Name == "Technician") ||
                !IsQualified(technician, claim.Warranty.RepairTicket.Device.DeviceTypeId))
                return await Rollback(tx, WarrantyWorkflowError.TechnicianNotQualified);
            claim.AssignedTechnicianId = technician.Id;
            claim.AssignedAt = DateTime.UtcNow;
            claim.Status = WarrantyClaimStatus.Assigned;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public async Task<WarrantyWorkflowError> ResolveClaimAsync(int claimId, int actorId, WarrantyClaimStatus status, string note, bool isAdmin)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(actorId)) return await Rollback(tx, WarrantyWorkflowError.InvalidActor);
        var claim = await db.WarrantyClaims.Include(x => x.Warranty).ThenInclude(x => x.RepairTicket).ThenInclude(x => x.Device)
            .Include(x => x.RepairTicketDetail)
            .Include(x => x.AssignedTechnician).ThenInclude(x => x!.DeviceTypes)
            .Include(x => x.AssignedTechnician).ThenInclude(x => x!.User)
            .FirstOrDefaultAsync(x => x.Id == claimId);
        if (claim is null) return await Rollback(tx, WarrantyWorkflowError.ClaimNotFound);
        if (claim.Status is not (WarrantyClaimStatus.Pending or WarrantyClaimStatus.Assigned)) return await Rollback(tx, WarrantyWorkflowError.ClaimNotPending);
        if (claim.RequestedByUserId == actorId) return await Rollback(tx, WarrantyWorkflowError.NotClaimOwner);
        if (!isAdmin)
        {
            var technician = await db.Technicians.Include(x => x.DeviceTypes)
                .FirstOrDefaultAsync(x => x.UserId == actorId && x.IsActive);
            if (technician is null || claim.Status != WarrantyClaimStatus.Assigned || claim.AssignedTechnicianId != technician.Id ||
                !IsQualified(technician, claim.Warranty.RepairTicket.Device.DeviceTypeId))
                return await Rollback(tx, WarrantyWorkflowError.TechnicianNotQualified);
        }
        if (status is not (WarrantyClaimStatus.Approved or WarrantyClaimStatus.Rejected))
            return await Rollback(tx, WarrantyWorkflowError.ClaimNotPending);

        if (status == WarrantyClaimStatus.Approved && claim.RepairTicketDetailId.HasValue)
        {
            var detail = claim.RepairTicketDetail;
            if (detail is null) return await Rollback(tx, WarrantyWorkflowError.ItemNotFound);
            if (claim.Warranty.Status == WarrantyStatus.Voided)
                return await Rollback(tx, WarrantyWorkflowError.WarrantyNotActive);
            if (!detail.WarrantyStartDate.HasValue || !detail.WarrantyEndDate.HasValue || detail.WarrantyDuration <= 0)
                return await Rollback(tx, WarrantyWorkflowError.ItemNotCovered);
            if (detail.WarrantyStartDate.Value.Date > claim.SubmittedAt.Date || detail.WarrantyEndDate.Value.Date < claim.SubmittedAt.Date)
                return await Rollback(tx, WarrantyWorkflowError.ItemWarrantyExpired);
        }
        else if (status == WarrantyClaimStatus.Approved)
        {
            if (claim.Warranty.Status == WarrantyStatus.Voided)
                return await Rollback(tx, WarrantyWorkflowError.WarrantyNotActive);
            if (claim.Warranty.StartDate.Date > claim.SubmittedAt.Date || claim.Warranty.EndDate.Date < claim.SubmittedAt.Date)
                return await Rollback(tx, WarrantyWorkflowError.WarrantyExpired);
        }

        claim.Status = status;
        claim.ResolutionNote = note.Trim();
        claim.ResolvedByUserId = actorId;
        claim.ResolvedAt = DateTime.UtcNow;
        if (status == WarrantyClaimStatus.Approved)
        {
            var originalTicket = claim.Warranty.RepairTicket;
            var itemDescription = claim.RepairTicketDetail?.Description ?? "Hạng mục bảo hành";
            var issue = $"Bảo hành hạng mục: {itemDescription}. Nội dung yêu cầu: {claim.Description}";
            var warrantyTicket = new RepairTicket
            {
                TicketNumber = $"RC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
                CustomerId = originalTicket.CustomerId,
                DeviceId = originalTicket.DeviceId,
                TechnicianId = claim.AssignedTechnician is { IsActive: true, User.IsActive: true } assigned &&
                    IsQualified(assigned, originalTicket.Device.DeviceTypeId) ? assigned.Id : null,
                ParentRepairTicketId = originalTicket.Id,
                IsWarrantyRepair = true,
                Status = RepairStatus.Received,
                IssueDescription = issue.Length <= 2000 ? issue : issue[..2000],
                CustomerNotes = $"Phiếu bảo hành phát sinh từ phiếu {originalTicket.TicketNumber}.",
                InternalNotes = $"Tạo tự động từ yêu cầu bảo hành #{claim.Id}. Ghi chú duyệt: {note.Trim()}",
                ReceivedAt = DateTime.UtcNow
            };
            await db.RepairTickets.AddAsync(warrantyTicket);
            claim.GeneratedRepairTicket = warrantyTicket;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public async Task<WarrantyWorkflowError> CancelClaimAsync(int claimId, int actorId, bool canManage)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(actorId)) return await Rollback(tx, WarrantyWorkflowError.InvalidActor);
        var claim = await db.WarrantyClaims.FirstOrDefaultAsync(x => x.Id == claimId);
        if (claim is null) return await Rollback(tx, WarrantyWorkflowError.ClaimNotFound);
        if (claim.Status is not (WarrantyClaimStatus.Pending or WarrantyClaimStatus.Assigned)) return await Rollback(tx, WarrantyWorkflowError.ClaimNotPending);
        if (claim.RequestedByUserId != actorId && !canManage) return await Rollback(tx, WarrantyWorkflowError.NotClaimOwner);
        claim.Status = WarrantyClaimStatus.Cancelled;
        claim.ResolvedByUserId = actorId;
        claim.ResolvedAt = DateTime.UtcNow;
        claim.ResolutionNote = "Yêu cầu đã được hủy.";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public async Task<WarrantyWorkflowError> VoidWarrantyAsync(int warrantyId, int actorId, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(actorId)) return await Rollback(tx, WarrantyWorkflowError.InvalidActor);
        var warranty = await db.Warranties.Include(x => x.Claims).FirstOrDefaultAsync(x => x.Id == warrantyId);
        if (warranty is null) return await Rollback(tx, WarrantyWorkflowError.WarrantyNotFound);
        if (warranty.Status != WarrantyStatus.Active) return await Rollback(tx, WarrantyWorkflowError.WarrantyNotActive);
        var now = DateTime.UtcNow;
        if (warranty.EndDate.Date < now.Date)
        {
            warranty.Status = WarrantyStatus.Expired;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return WarrantyWorkflowError.WarrantyExpired;
        }
        warranty.Status = WarrantyStatus.Voided;
        warranty.VoidedAt = now;
        warranty.VoidedByUserId = actorId;
        warranty.VoidReason = reason.Trim();
        foreach (var pending in warranty.Claims.Where(x => x.Status is WarrantyClaimStatus.Pending or WarrantyClaimStatus.Assigned))
        {
            pending.Status = WarrantyClaimStatus.Cancelled;
            pending.ResolvedByUserId = actorId;
            pending.ResolvedAt = now;
            pending.ResolutionNote = "Bảo hành đã bị hủy: " + reason.Trim();
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return WarrantyWorkflowError.None;
    }

    public Task<int> MarkExpiredWarrantiesAsync(DateTime today) => db.Warranties
        .Where(x => x.Status == WarrantyStatus.Active && x.EndDate < today)
        .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, WarrantyStatus.Expired));

    private Task<bool> IsActiveUserAsync(int userId) => db.Users.AnyAsync(x => x.Id == userId && x.IsActive);
    private static bool IsQualified(Technician technician, int deviceTypeId) =>
        technician.IsActive && technician.DeviceTypes.Any(x => x.DeviceTypeId == deviceTypeId);

    private static bool IsCoveredAtSubmission(WarrantyClaim claim)
    {
        var detail = claim.RepairTicketDetail;
        return detail is not null && detail.WarrantyStartDate.HasValue && detail.WarrantyEndDate.HasValue &&
            detail.WarrantyDuration > 0 && detail.WarrantyStartDate.Value.Date <= claim.SubmittedAt.Date &&
            detail.WarrantyEndDate.Value.Date >= claim.SubmittedAt.Date;
    }
    private static async Task<WarrantyWorkflowError> Rollback(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, WarrantyWorkflowError error)
    {
        await tx.RollbackAsync();
        return error;
    }
}
