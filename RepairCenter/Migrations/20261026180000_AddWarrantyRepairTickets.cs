using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    public partial class AddWarrantyRepairTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentRepairTicketId",
                table: "RepairTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWarrantyRepair",
                table: "RepairTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GeneratedRepairTicketId",
                table: "WarrantyClaims",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_ParentRepairTicketId",
                table: "RepairTickets",
                column: "ParentRepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_GeneratedRepairTicketId",
                table: "WarrantyClaims",
                column: "GeneratedRepairTicketId",
                unique: true,
                filter: "[GeneratedRepairTicketId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairTickets_RepairTickets_ParentRepairTicketId",
                table: "RepairTickets",
                column: "ParentRepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_RepairTickets_GeneratedRepairTicketId",
                table: "WarrantyClaims",
                column: "GeneratedRepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
DECLARE @CreatedWarrantyTickets TABLE (ClaimId int NOT NULL, RepairTicketId int NOT NULL);

MERGE RepairTickets AS target
USING
(
    SELECT claim.Id AS ClaimId,
           warranty.RepairTicketId AS ParentRepairTicketId,
           original.CustomerId,
           original.DeviceId,
           original.TechnicianId,
           CONCAT(N'Bảo hành hạng mục: ', COALESCE(detail.Description, N'Hạng mục bảo hành'), N'. Nội dung yêu cầu: ', claim.Description) AS IssueDescription,
           CONCAT(N'Phiếu bảo hành phát sinh từ phiếu ', original.TicketNumber, N'.') AS CustomerNotes,
           CONCAT(N'Tạo tự động từ yêu cầu bảo hành #', claim.Id, N'. Ghi chú duyệt: ', COALESCE(claim.ResolutionNote, N'')) AS InternalNotes
    FROM WarrantyClaims AS claim
    INNER JOIN Warranties AS warranty ON warranty.Id = claim.WarrantyId
    INNER JOIN RepairTickets AS original ON original.Id = warranty.RepairTicketId
    LEFT JOIN RepairTicketDetails AS detail ON detail.Id = claim.RepairTicketDetailId
    WHERE claim.Status = 2 AND claim.GeneratedRepairTicketId IS NULL
) AS source
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (TicketNumber, CustomerId, DeviceId, TechnicianId, ParentRepairTicketId, IsWarrantyRepair, Status, IssueDescription, CustomerNotes, InternalNotes, ReceivedAt)
    VALUES
    (
        CONCAT('RC-', CONVERT(char(8), GETUTCDATE(), 112), '-', LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 9)),
        source.CustomerId,
        source.DeviceId,
        source.TechnicianId,
        source.ParentRepairTicketId,
        1,
        1,
        LEFT(source.IssueDescription, 2000),
        source.CustomerNotes,
        LEFT(source.InternalNotes, 2000),
        GETUTCDATE()
    )
OUTPUT source.ClaimId, inserted.Id INTO @CreatedWarrantyTickets (ClaimId, RepairTicketId);

UPDATE claim
SET GeneratedRepairTicketId = created.RepairTicketId
FROM WarrantyClaims AS claim
INNER JOIN @CreatedWarrantyTickets AS created ON created.ClaimId = claim.Id;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepairTickets_RepairTickets_ParentRepairTicketId",
                table: "RepairTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_RepairTickets_GeneratedRepairTicketId",
                table: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_RepairTickets_ParentRepairTicketId",
                table: "RepairTickets");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_GeneratedRepairTicketId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ParentRepairTicketId",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "IsWarrantyRepair",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "GeneratedRepairTicketId",
                table: "WarrantyClaims");
        }
    }
}
