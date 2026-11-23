using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Auth;
public class ChangeTemporaryPasswordRequest
{
    [Required, StringLength(200)] public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(200, MinimumLength = 12)] public string NewPassword { get; set; } = string.Empty;
}
