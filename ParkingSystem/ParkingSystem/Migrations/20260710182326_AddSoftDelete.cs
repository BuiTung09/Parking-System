using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParkingLog_Status",
                table: "ParkingLogs");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParkingLog_Status",
                table: "ParkingLogs",
                sql: "[Status] IN ('Inside', 'Outside', 'Waiting')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParkingLog_Status",
                table: "ParkingLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vehicles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParkingLog_Status",
                table: "ParkingLogs",
                sql: "[Status] IN ('Inside', 'Outside')");
        }
    }
}
