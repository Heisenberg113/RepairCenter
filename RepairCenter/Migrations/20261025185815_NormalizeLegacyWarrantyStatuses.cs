using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyWarrantyStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Warranties
                SET Status = CASE
                    WHEN EndDate >= CAST(GETUTCDATE() AS date) THEN 1
                    ELSE 2
                END
                WHERE Status = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
