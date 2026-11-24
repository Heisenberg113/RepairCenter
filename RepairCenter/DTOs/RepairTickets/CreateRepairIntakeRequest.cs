using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.RepairTickets;

public sealed class CreateRepairIntakeRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? CustomerId { get; set; }

    public IntakeCustomerRequest? NewCustomer { get; set; }

    [Range(1, int.MaxValue)]
    public int? DeviceId { get; set; }

    public IntakeDeviceRequest? NewDevice { get; set; }

    [Required, StringLength(2000, MinimumLength = 1)]
    public string IssueDescription { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? CustomerNotes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((CustomerId.HasValue && NewCustomer is not null) ||
            (!CustomerId.HasValue && NewCustomer is null))
            yield return new ValidationResult("Chọn khách hàng có sẵn hoặc nhập khách hàng mới.", [nameof(CustomerId), nameof(NewCustomer)]);

        if ((DeviceId.HasValue && NewDevice is not null) ||
            (!DeviceId.HasValue && NewDevice is null))
            yield return new ValidationResult("Chọn thiết bị có sẵn hoặc nhập thiết bị mới.", [nameof(DeviceId), nameof(NewDevice)]);
    }
}

public sealed class IntakeCustomerRequest
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

public sealed class IntakeDeviceRequest
{
    [Range(1, int.MaxValue)]
    public int DeviceTypeId { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}
