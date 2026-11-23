using Microsoft.EntityFrameworkCore;
using RepairCenter.Entities;
using RepairCenter.Enums;

namespace RepairCenter.Data;

public class RepairCenterDbContext(DbContextOptions<RepairCenterDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<TechnicianDeviceType> TechnicianDeviceTypes => Set<TechnicianDeviceType>();
    public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();
    public DbSet<RepairTicketDetail> RepairTicketDetails => Set<RepairTicketDetail>();
    public DbSet<RepairService> RepairServices => Set<RepairService>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RepairTicketStatusHistory> RepairTicketStatusHistories => Set<RepairTicketStatusHistory>();
    public DbSet<CatalogChangeRequest> CatalogChangeRequests => Set<CatalogChangeRequest>();
    public DbSet<RepairQuoteVersion> RepairQuoteVersions => Set<RepairQuoteVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(300);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(x => x.Brand).HasMaxLength(100);
            entity.Property(x => x.Model).HasMaxLength(100);
            entity.Property(x => x.SerialNumber).HasMaxLength(100);
            entity.HasIndex(x => x.SerialNumber);
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeviceType)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeviceType>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TechnicianDeviceType>(entity =>
        {
            entity.HasKey(x => new { x.TechnicianId, x.DeviceTypeId });
            entity.HasOne(x => x.Technician)
                .WithMany(x => x.DeviceTypes)
                .HasForeignKey(x => x.TechnicianId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.DeviceType)
                .WithMany(x => x.Technicians)
                .HasForeignKey(x => x.DeviceTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RepairTicket>(entity =>
        {
            entity.Property(x => x.TicketNumber).HasMaxLength(30);
            entity.Property(x => x.IssueDescription).HasMaxLength(2000);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.ActualCost).HasPrecision(18, 2);
            entity.HasIndex(x => x.TicketNumber).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.RepairTickets)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device)
                .WithMany(x => x.RepairTickets)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Technician)
                .WithMany(x => x.RepairTickets)
                .HasForeignKey(x => x.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RepairQuoteVersion>(entity =>
        {
            entity.Property(x => x.SnapshotJson).HasMaxLength(20000);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64);
            entity.Property(x => x.Response).HasMaxLength(30);
            entity.Property(x => x.ResponseChannel).HasMaxLength(40);
            entity.Property(x => x.ResponseNote).HasMaxLength(1000);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.RepairTicketId, x.VersionNumber }).IsUnique();
            entity.HasOne(x => x.RepairTicket).WithMany().HasForeignKey(x => x.RepairTicketId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RespondedByUser).WithMany().HasForeignKey(x => x.RespondedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairTicketStatusHistory>(entity =>
        {
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => new { x.RepairTicketId, x.ChangedAt });
            entity.HasOne(x => x.RepairTicket)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.RepairTicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.StatusChanges)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairTicketDetail>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_RepairTicketDetails_ItemType",
                """
                ([RepairServiceId] IS NOT NULL AND [SparePartId] IS NULL)
                OR
                ([RepairServiceId] IS NULL AND [SparePartId] IS NOT NULL)
                """));
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.WarrantyTerms).HasMaxLength(1000);
            entity.HasOne(x => x.RepairTicket)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.RepairTicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RepairService)
                .WithMany(x => x.RepairTicketDetails)
                .HasForeignKey(x => x.RepairServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SparePart)
                .WithMany(x => x.RepairTicketDetails)
                .HasForeignKey(x => x.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.WarrantyClaims).WithOne(x => x.RepairTicketDetail)
                .HasForeignKey(x => x.RepairTicketDetailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairService>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.WarrantyTerms).HasMaxLength(1000);
            entity.HasOne(x => x.DeviceType).WithMany(x => x.RepairServices)
                .HasForeignKey(x => x.DeviceTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.DeviceTypeId);
        });

        modelBuilder.Entity<Technician>(entity =>
        {
            entity.Property(x => x.EmployeeCode).HasMaxLength(30);
            entity.Property(x => x.FullName).HasMaxLength(150);
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.HasOne(x => x.User)
                .WithOne(x => x.Technician)
                .HasForeignKey<Technician>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SparePart>(entity =>
        {
            entity.Property(x => x.PartNumber).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.SellingPrice).HasPrecision(18, 2);
            entity.Property(x => x.QuantityInStock).HasPrecision(18, 2);
            entity.Property(x => x.ReorderLevel).HasPrecision(18, 2);
            entity.Property(x => x.WarrantyTerms).HasMaxLength(1000);
            entity.HasOne(x => x.DeviceType).WithMany(x => x.SpareParts)
                .HasForeignKey(x => x.DeviceTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.DeviceTypeId);
            entity.HasIndex(x => x.PartNumber).IsUnique();
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_SpareParts_UnitCost",
                    "[UnitCost] >= 0");

                t.HasCheckConstraint(
                    "CK_SpareParts_SellingPrice",
                    "[SellingPrice] >= 0");

                t.HasCheckConstraint(
                    "CK_SpareParts_QuantityInStock",
                    "[QuantityInStock] >= 0");

                t.HasCheckConstraint(
                    "CK_SpareParts_ReorderLevel",
                    "[ReorderLevel] >= 0");
            });
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.Property(x => x.TransactionType).HasMaxLength(30);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.HasOne(x => x.SparePart)
                .WithMany(x => x.InventoryTransactions)
                .HasForeignKey(x => x.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(x => x.InvoiceNumber).HasMaxLength(30);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            entity.Property(x => x.TaxPercent).HasPrecision(9, 4);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.AmountPaid).HasPrecision(18, 2);
            entity.Property(x => x.DueAt);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasOne(x => x.RepairTicket)
                .WithOne(x => x.Invoice)
                .HasForeignKey<Invoice>(x => x.RepairTicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Invoices_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [AmountPaid] >= 0 AND [AmountPaid] <= [TotalAmount]");
            });
        });

        modelBuilder.Entity<InvoicePayment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.PaymentMethod).HasMaxLength(40);
            entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(InvoicePaymentType.Payment)
                .HasSentinel(InvoicePaymentType.Payment);
            entity.HasOne(x => x.Invoice)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProcessedByUser)
                .WithMany()
                .HasForeignKey(x => x.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RefundOfPayment)
                .WithMany(x => x.Refunds)
                .HasForeignKey(x => x.RefundOfPaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_InvoicePayments_Amount", "[Amount] > 0");
                t.HasCheckConstraint("CK_InvoicePayments_TypeAndRefund", "([Type] = 'Payment' AND [RefundOfPaymentId] IS NULL) OR ([Type] = 'Refund' AND [RefundOfPaymentId] IS NOT NULL)");
            });
        });

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.Property(x => x.WarrantyCode).HasMaxLength(30);
            entity.Property(x => x.Terms).HasMaxLength(1000);
            entity.Property(x => x.VoidReason).HasMaxLength(1000);
            entity.HasIndex(x => x.WarrantyCode).IsUnique();
            entity.HasOne(x => x.RepairTicket)
                .WithOne(x => x.Warranty)
                .HasForeignKey<Warranty>(x => x.RepairTicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.VoidedByUser).WithMany()
                .HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ResolutionNote).HasMaxLength(1000);
            entity.HasIndex(x => new { x.WarrantyId, x.SubmittedAt });
            entity.HasIndex(x => new { x.Status, x.AssignedTechnicianId });
            entity.HasIndex(x => x.GeneratedRepairTicketId).IsUnique().HasFilter("[GeneratedRepairTicketId] IS NOT NULL");
            entity.HasOne(x => x.Warranty).WithMany(x => x.Claims)
                .HasForeignKey(x => x.WarrantyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestedByUser).WithMany()
                .HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedByUser).WithMany()
                .HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedTechnician).WithMany()
                .HasForeignKey(x => x.AssignedTechnicianId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GeneratedRepairTicket).WithOne()
                .HasForeignKey<WarrantyClaim>(x => x.GeneratedRepairTicketId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairTicket>(entity =>
        {
            entity.HasOne(x => x.ParentRepairTicket).WithMany(x => x.WarrantyRepairTickets)
                .HasForeignKey(x => x.ParentRepairTicketId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.UserName).HasMaxLength(100);
            entity.Property(x => x.FullName).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.Property(x => x.MustChangePassword).HasDefaultValue(false);
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogChangeRequest>(entity =>
        {
            entity.Property(x => x.ItemType).HasMaxLength(20);
            entity.Property(x => x.Action).HasMaxLength(20);
            entity.Property(x => x.PayloadJson).HasMaxLength(4000);
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.Property(x => x.ResolutionNote).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Status, x.RequestedAt });
            entity.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
