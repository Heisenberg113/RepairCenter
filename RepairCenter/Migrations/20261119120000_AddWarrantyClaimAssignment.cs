using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RepairCenter.Data;

#nullable disable

namespace RepairCenter.Migrations;

[DbContext(typeof(RepairCenterDbContext))]
[Migration("20261119120000_AddWarrantyClaimAssignment")]
public partial class AddWarrantyClaimAssignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "AssignedAt", table: "WarrantyClaims", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "AssignedTechnicianId", table: "WarrantyClaims", type: "int", nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_WarrantyClaims_Status_AssignedTechnicianId",
            table: "WarrantyClaims", columns: new[] { "Status", "AssignedTechnicianId" });
        migrationBuilder.CreateIndex(
            name: "IX_WarrantyClaims_AssignedTechnicianId",
            table: "WarrantyClaims", column: "AssignedTechnicianId");
        migrationBuilder.AddForeignKey(
            name: "FK_WarrantyClaims_Technicians_AssignedTechnicianId",
            table: "WarrantyClaims", column: "AssignedTechnicianId",
            principalTable: "Technicians", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_WarrantyClaims_Technicians_AssignedTechnicianId", table: "WarrantyClaims");
        migrationBuilder.DropIndex(name: "IX_WarrantyClaims_Status_AssignedTechnicianId", table: "WarrantyClaims");
        migrationBuilder.DropIndex(name: "IX_WarrantyClaims_AssignedTechnicianId", table: "WarrantyClaims");
        migrationBuilder.DropColumn(name: "AssignedAt", table: "WarrantyClaims");
        migrationBuilder.DropColumn(name: "AssignedTechnicianId", table: "WarrantyClaims");
    }
}
