using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RepairCenter.Data;

#nullable disable

namespace RepairCenter.Migrations;

[DbContext(typeof(RepairCenterDbContext))]
[Migration("20261124150000_BackfillQuoteChangeHistory")]
public partial class BackfillQuoteChangeHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH RankedQuotes AS
            (
                SELECT
                    quote.Id,
                    quote.RepairTicketId,
                    quote.SnapshotHash,
                    quote.CreatedByUserId,
                    quote.CreatedAt,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY quote.RepairTicketId, quote.SnapshotHash
                        ORDER BY quote.VersionNumber, quote.Id
                    ) AS SnapshotOccurrence
                FROM dbo.RepairQuoteVersions AS quote
            )
            INSERT INTO dbo.RepairTicketStatusHistories
                (RepairTicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Note)
            SELECT
                quote.RepairTicketId,
                ticket.Status,
                ticket.Status,
                quote.CreatedByUserId,
                quote.CreatedAt,
                N'Thay đổi báo giá'
            FROM RankedQuotes AS quote
            INNER JOIN dbo.RepairTickets AS ticket ON ticket.Id = quote.RepairTicketId
            WHERE quote.SnapshotOccurrence = 1
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RepairTicketStatusHistories AS history
                    WHERE history.RepairTicketId = quote.RepairTicketId
                        AND history.ChangedByUserId = quote.CreatedByUserId
                        AND history.ChangedAt = quote.CreatedAt
                        AND history.Note = N'Thay đổi báo giá'
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
