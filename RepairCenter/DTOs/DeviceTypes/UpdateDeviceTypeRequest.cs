using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.DeviceTypes;

public class UpdateDeviceTypeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
