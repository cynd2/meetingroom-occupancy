using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingRoomSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitCleanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalendarId",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "Organizer",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "RoomName",
                table: "CalendarBookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarId",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Organizer",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoomName",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
