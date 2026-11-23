using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    public partial class AddDeviceTypeToRepairCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceTypeId",
                table: "RepairServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceTypeId",
                table: "SpareParts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairServices_DeviceTypeId",
                table: "RepairServices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_DeviceTypeId",
                table: "SpareParts",
                column: "DeviceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairServices_DeviceTypes_DeviceTypeId",
                table: "RepairServices",
                column: "DeviceTypeId",
                principalTable: "DeviceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpareParts_DeviceTypes_DeviceTypeId",
                table: "SpareParts",
                column: "DeviceTypeId",
                principalTable: "DeviceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_RepairServices_DeviceTypes_DeviceTypeId", table: "RepairServices");
            migrationBuilder.DropForeignKey(name: "FK_SpareParts_DeviceTypes_DeviceTypeId", table: "SpareParts");
            migrationBuilder.DropIndex(name: "IX_RepairServices_DeviceTypeId", table: "RepairServices");
            migrationBuilder.DropIndex(name: "IX_SpareParts_DeviceTypeId", table: "SpareParts");
            migrationBuilder.DropColumn(name: "DeviceTypeId", table: "RepairServices");
            migrationBuilder.DropColumn(name: "DeviceTypeId", table: "SpareParts");
        }
    }
}
