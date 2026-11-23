namespace RepairCenter.Entities;

public class User
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Technician? Technician { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RepairTicketStatusHistory> StatusChanges { get; set; } = new List<RepairTicketStatusHistory>();
}
