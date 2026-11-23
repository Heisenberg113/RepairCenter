using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairCenter.Migrations
{
    public partial class AddDeviceTypesAndTechnicianDeviceTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DeviceTypes", x => x.Id));

            migrationBuilder.AddColumn<int>(
                name: "DeviceTypeId",
                table: "Devices",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [Devices]
                    WHERE [DeviceType] IS NULL OR LTRIM(RTRIM([DeviceType])) = ''
                )
                    THROW 51000, 'Cannot migrate Devices: DeviceType contains empty values.', 1;

                INSERT INTO [DeviceTypes] ([Name])
                SELECT DISTINCT LTRIM(RTRIM([DeviceType]))
                FROM [Devices];

                UPDATE d
                SET [DeviceTypeId] = t.[Id]
                FROM [Devices] AS d
                INNER JOIN [DeviceTypes] AS t
                    ON t.[Name] = LTRIM(RTRIM(d.[DeviceType]));

                IF EXISTS (SELECT 1 FROM [Devices] WHERE [DeviceTypeId] IS NULL)
                    THROW 51001, 'Cannot migrate Devices: one or more DeviceType values were not mapped.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceTypeId",
                table: "Devices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "DeviceType", table: "Devices");

            migrationBuilder.CreateTable(
                name: "TechnicianDeviceTypes",
                columns: table => new
                {
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    DeviceTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianDeviceTypes", x => new { x.TechnicianId, x.DeviceTypeId });
                    table.ForeignKey(
                        name: "FK_TechnicianDeviceTypes_DeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "DeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianDeviceTypes_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Devices_DeviceTypeId", "Devices", "DeviceTypeId");
            migrationBuilder.CreateIndex("IX_DeviceTypes_Name", "DeviceTypes", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_TechnicianDeviceTypes_DeviceTypeId", "TechnicianDeviceTypes", "DeviceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceTypes_DeviceTypeId",
                table: "Devices",
                column: "DeviceTypeId",
                principalTable: "DeviceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Devices_DeviceTypes_DeviceTypeId", "Devices");
            migrationBuilder.DropTable("TechnicianDeviceTypes");
            migrationBuilder.DropIndex("IX_Devices_DeviceTypeId", "Devices");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE d
                SET [DeviceType] = t.[Name]
                FROM [Devices] AS d
                INNER JOIN [DeviceTypes] AS t ON t.[Id] = d.[DeviceTypeId];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceType",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn("DeviceTypeId", "Devices");
            migrationBuilder.DropTable("DeviceTypes");
        }
    }
}
