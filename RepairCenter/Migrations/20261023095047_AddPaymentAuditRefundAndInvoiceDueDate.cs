using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAuditRefundAndInvoiceDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Invoices] SET [DueAt] = DATEADD(day, 30, [IssuedAt]) WHERE [DueAt] IS NULL");

            migrationBuilder.AddColumn<int>(
                name: "ProcessedByUserId",
                table: "InvoicePayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundOfPaymentId",
                table: "InvoicePayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InvoicePayments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Payment");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_ProcessedByUserId",
                table: "InvoicePayments",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_RefundOfPaymentId",
                table: "InvoicePayments",
                column: "RefundOfPaymentId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InvoicePayments_TypeAndRefund",
                table: "InvoicePayments",
                sql: "([Type] = 'Payment' AND [RefundOfPaymentId] IS NULL) OR ([Type] = 'Refund' AND [RefundOfPaymentId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoicePayments_InvoicePayments_RefundOfPaymentId",
                table: "InvoicePayments",
                column: "RefundOfPaymentId",
                principalTable: "InvoicePayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoicePayments_Users_ProcessedByUserId",
                table: "InvoicePayments",
                column: "ProcessedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoicePayments_InvoicePayments_RefundOfPaymentId",
                table: "InvoicePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoicePayments_Users_ProcessedByUserId",
                table: "InvoicePayments");

            migrationBuilder.DropIndex(
                name: "IX_InvoicePayments_ProcessedByUserId",
                table: "InvoicePayments");

            migrationBuilder.DropIndex(
                name: "IX_InvoicePayments_RefundOfPaymentId",
                table: "InvoicePayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InvoicePayments_TypeAndRefund",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "RefundOfPaymentId",
                table: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "InvoicePayments");
        }
    }
}
