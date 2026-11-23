using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyClaimsAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Warranties",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Warranties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "Warranties",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarrantyClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarrantyId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_VoidedByUserId",
                table: "Warranties",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_RequestedByUserId",
                table: "WarrantyClaims",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ResolvedByUserId",
                table: "WarrantyClaims",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_WarrantyId_SubmittedAt",
                table: "WarrantyClaims",
                columns: new[] { "WarrantyId", "SubmittedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_Users_VoidedByUserId",
                table: "Warranties",
                column: "VoidedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_Users_VoidedByUserId",
                table: "Warranties");

            migrationBuilder.DropTable(
                name: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_Warranties_VoidedByUserId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Warranties");
        }
    }
}
