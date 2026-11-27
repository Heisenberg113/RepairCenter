using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Accounts;

public sealed class UpdateAccountRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }
}
