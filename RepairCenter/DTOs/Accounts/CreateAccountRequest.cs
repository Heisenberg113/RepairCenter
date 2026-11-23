using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Accounts;
public class CreateAccountRequest
{
    [Required, StringLength(100, MinimumLength = 3)] public string UserName { get; set; } = string.Empty;
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [StringLength(30)] public string? PhoneNumber { get; set; }
    [Required, StringLength(200, MinimumLength = 12)] public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
