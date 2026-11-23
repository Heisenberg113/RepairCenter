using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Customers;

public class CreateCustomerRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}
