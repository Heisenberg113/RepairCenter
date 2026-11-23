using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Accounts;
public class UpdateAccountRolesRequest
{
    [Required] public List<string> Roles { get; set; } = [];
}
