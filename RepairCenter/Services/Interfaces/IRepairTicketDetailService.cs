using RepairCenter.Entities;
using RepairCenter.Enums;

namespace RepairCenter.Services.Interfaces;

public interface IRepairTicketDetailService
{
    Task<RepairTicketDetailResult<List<RepairTicketDetail>>> GetByTicketAsync(int ticketId);
    Task<RepairTicketDetailResult<RepairTicketDetail>> AddRepairServiceAsync(
        int ticketId, int serviceId, decimal quantity, decimal discountPercent,
        int? warrantyDuration = null, WarrantyDurationUnit? warrantyDurationUnit = null, string? warrantyTerms = null);
    Task<RepairTicketDetailResult<RepairTicketDetail>> AddSparePartAsync(
        int ticketId, int sparePartId, decimal quantity, decimal discountPercent,
        int? warrantyDuration = null, WarrantyDurationUnit? warrantyDurationUnit = null, string? warrantyTerms = null);
    Task<RepairTicketDetailError> UpdateAsync(int ticketId, int detailId, decimal quantity, decimal discountPercent);
    Task<RepairTicketDetailError> DeleteAsync(int ticketId, int detailId);
}
