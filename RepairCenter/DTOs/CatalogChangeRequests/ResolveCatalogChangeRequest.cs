using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.CatalogChangeRequests;

public class ResolveCatalogChangeRequest
{
    [StringLength(1000)]
    public string? Note { get; set; }
}
