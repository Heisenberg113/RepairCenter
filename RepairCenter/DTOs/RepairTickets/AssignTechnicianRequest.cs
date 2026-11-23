using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.RepairTickets;

public class AssignTechnicianRequest
{
    [Range(1, int.MaxValue)]
    public int TechnicianId { get; set; }
}
