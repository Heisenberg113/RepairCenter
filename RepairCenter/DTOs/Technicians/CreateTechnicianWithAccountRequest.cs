using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Technicians;

public sealed class CreateTechnicianWithAccountRequest : CreateTechnicianRequest, IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("Email bắt buộc khi tạo tài khoản đăng nhập.", [nameof(Email)]);

        if (!IsActive)
            yield return new ValidationResult("Chỉ có thể tạo tài khoản cho kỹ thuật viên đang hoạt động.", [nameof(IsActive)]);
    }
}
