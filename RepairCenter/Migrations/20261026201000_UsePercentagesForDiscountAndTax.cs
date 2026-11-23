using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    public partial class UsePercentagesForDiscountAndTax : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "RepairTicketDetails",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Invoices",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercent",
                table: "Invoices",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
UPDATE RepairTicketDetails
SET DiscountPercent = CASE
    WHEN Quantity * UnitPrice = 0 THEN 0
    ELSE CAST(ROUND(DiscountAmount * 100.0 / (Quantity * UnitPrice), 4) AS decimal(9,4))
END;

UPDATE Invoices
SET DiscountPercent = CASE
        WHEN Subtotal = 0 THEN 0
        ELSE CAST(ROUND(DiscountAmount * 100.0 / Subtotal, 4) AS decimal(9,4))
    END,
    TaxPercent = CASE
        WHEN Subtotal - DiscountAmount <= 0 THEN 0
        ELSE CAST(ROUND(TaxAmount * 100.0 / (Subtotal - DiscountAmount), 4) AS decimal(9,4))
    END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DiscountPercent", table: "RepairTicketDetails");
            migrationBuilder.DropColumn(name: "DiscountPercent", table: "Invoices");
            migrationBuilder.DropColumn(name: "TaxPercent", table: "Invoices");
        }
    }
}
