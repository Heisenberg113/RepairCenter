using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRepairServiceNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT [Name] FROM [RepairServices] GROUP BY [Name] HAVING COUNT(*) > 1) " +
                "BEGIN THROW 51000, 'Resolve duplicate repair service names before applying this migration.', 1; END;");
            migrationBuilder.CreateIndex(
                name: "IX_RepairServices_Name",
                table: "RepairServices",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepairServices_Name",
                table: "RepairServices");
        }
    }
}
