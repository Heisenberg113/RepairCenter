using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public enum AddTicketDetailError
{
    None,
    RepairTicketNotFound,
    RepairTicketClosed,
    RepairServiceNotFoundOrInactive,
    DeviceTypeMismatch,
    SparePartNotFound,
    SparePartInactive,
    InsufficientStock,
    ChangedByUserNotFoundOrInactive
}

public enum EditTicketDetailError { None, RepairTicketNotFound, RepairTicketNotEditable, DetailNotFound, InsufficientStock, InvalidAmounts, SparePartInactive, ChangedByUserNotFoundOrInactive }

public interface IRepairTicketDetailRepository
{
    Task<bool> TicketExistsAsync(int repairTicketId);
    Task<RepairTicket?> GetTicketAsync(int repairTicketId);
    Task<List<RepairTicketDetail>> GetByTicketAsync(int repairTicketId);
    Task<RepairService?> GetActiveRepairServiceAsync(int repairServiceId);
    Task<SparePart?> GetSparePartAsync(int sparePartId);
    Task<AddTicketDetailError> AddAsync(RepairTicketDetail detail, InventoryTransaction? inventoryTransaction, int changedByUserId, string statusNote);
    Task<EditTicketDetailError> UpdateAsync(int ticketId, int detailId, decimal quantity, decimal discountPercent, int changedByUserId);
    Task<EditTicketDetailError> DeleteAsync(int ticketId, int detailId, int changedByUserId);
}
