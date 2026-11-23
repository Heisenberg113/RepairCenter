using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddPerItemWarrantyPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepairTicketDetailId",
                table: "WarrantyClaims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDuration",
                table: "SpareParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDurationUnit",
                table: "SpareParts",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "SpareParts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDuration",
                table: "RepairTicketDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDurationUnit",
                table: "RepairTicketDetails",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyEndDate",
                table: "RepairTicketDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyStartDate",
                table: "RepairTicketDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "RepairTicketDetails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDuration",
                table: "RepairServices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyDurationUnit",
                table: "RepairServices",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyTerms",
                table: "RepairServices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE detail
                SET WarrantyDuration = CASE WHEN DATEDIFF(day, warranty.StartDate, warranty.EndDate) < 1 THEN 1
                                            ELSE DATEDIFF(day, warranty.StartDate, warranty.EndDate) END,
                    WarrantyDurationUnit = 1,
                    WarrantyTerms = warranty.Terms,
                    WarrantyStartDate = warranty.StartDate,
                    WarrantyEndDate = warranty.EndDate
                FROM RepairTicketDetails AS detail
                INNER JOIN Warranties AS warranty ON warranty.RepairTicketId = detail.RepairTicketId;

                -- A previous approved ticket-wide claim marked the whole warranty as claimed.
                -- The new model tracks claims against individual ticket lines.
                UPDATE Warranties SET Status = 1 WHERE Status = 3 AND EndDate >= CAST(GETUTCDATE() AS date);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_RepairTicketDetailId",
                table: "WarrantyClaims",
                column: "RepairTicketDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_RepairTicketDetails_RepairTicketDetailId",
                table: "WarrantyClaims",
                column: "RepairTicketDetailId",
                principalTable: "RepairTicketDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_RepairTicketDetails_RepairTicketDetailId",
                table: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_RepairTicketDetailId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "RepairTicketDetailId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "WarrantyDuration",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "WarrantyDurationUnit",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "WarrantyDuration",
                table: "RepairTicketDetails");

            migrationBuilder.DropColumn(
                name: "WarrantyDurationUnit",
                table: "RepairTicketDetails");

            migrationBuilder.DropColumn(
                name: "WarrantyEndDate",
                table: "RepairTicketDetails");

            migrationBuilder.DropColumn(
                name: "WarrantyStartDate",
                table: "RepairTicketDetails");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "RepairTicketDetails");

            migrationBuilder.DropColumn(
                name: "WarrantyDuration",
                table: "RepairServices");

            migrationBuilder.DropColumn(
                name: "WarrantyDurationUnit",
                table: "RepairServices");

            migrationBuilder.DropColumn(
                name: "WarrantyTerms",
                table: "RepairServices");
        }
    }
}
