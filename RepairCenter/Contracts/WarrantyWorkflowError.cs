namespace RepairCenter.Contracts;

public enum WarrantyWorkflowError
{
    None,
    WarrantyNotFound,
    WarrantyNotActive,
    WarrantyExpired,
    PendingClaimExists,
    ClaimNotFound,
    ClaimNotPending,
    NotClaimOwner,
    InvalidActor,
    InvalidRequest,
    ItemNotFound,
    ItemNotCovered,
    ItemWarrantyExpired,
    TechnicianNotQualified,
    ClaimAlreadyAssigned
}
