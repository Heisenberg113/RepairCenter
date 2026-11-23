using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_RepairTickets_RepairTicketId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_RepairTickets_RepairTicketId",
                table: "Warranties");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SpareParts_QuantityInStock",
                table: "SpareParts",
                sql: "[QuantityInStock] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SpareParts_ReorderLevel",
                table: "SpareParts",
                sql: "[ReorderLevel] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SpareParts_SellingPrice",
                table: "SpareParts",
                sql: "[SellingPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SpareParts_UnitCost",
                table: "SpareParts",
                sql: "[UnitCost] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RepairTicketDetails_ItemType",
                table: "RepairTicketDetails",
                sql: "([RepairServiceId] IS NOT NULL AND [SparePartId] IS NULL)\r\nOR\r\n([RepairServiceId] IS NULL AND [SparePartId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_RepairTickets_RepairTicketId",
                table: "Invoices",
                column: "RepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_RepairTickets_RepairTicketId",
                table: "Warranties",
                column: "RepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_RepairTickets_RepairTicketId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_RepairTickets_RepairTicketId",
                table: "Warranties");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SpareParts_QuantityInStock",
                table: "SpareParts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SpareParts_ReorderLevel",
                table: "SpareParts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SpareParts_SellingPrice",
                table: "SpareParts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SpareParts_UnitCost",
                table: "SpareParts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RepairTicketDetails_ItemType",
                table: "RepairTicketDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_RepairTickets_RepairTicketId",
                table: "Invoices",
                column: "RepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_RepairTickets_RepairTicketId",
                table: "Warranties",
                column: "RepairTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
