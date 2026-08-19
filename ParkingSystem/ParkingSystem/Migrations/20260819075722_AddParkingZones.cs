using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "ParkingLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParkingZones",
                columns: table => new
                {
                    ZoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalCarSlots = table.Column<int>(type: "int", nullable: false),
                    TotalBikeSlots = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingZones", x => x.ZoneId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingLogs_ZoneId",
                table: "ParkingLogs",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingLogs_ParkingZones_ZoneId",
                table: "ParkingLogs",
                column: "ZoneId",
                principalTable: "ParkingZones",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingLogs_ParkingZones_ZoneId",
                table: "ParkingLogs");

            migrationBuilder.DropTable(
                name: "ParkingZones");

            migrationBuilder.DropIndex(
                name: "IX_ParkingLogs_ZoneId",
                table: "ParkingLogs");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "ParkingLogs");
        }
    }
}
