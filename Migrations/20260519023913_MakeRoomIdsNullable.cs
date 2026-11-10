using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingRoomSystem.Migrations
{
    /// <inheritdoc />
    public partial class MakeRoomIdsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_CalendarRoomId",
                table: "MeetingRooms");

            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_CameraRoomId",
                table: "MeetingRooms");

            migrationBuilder.AlterColumn<string>(
                name: "CameraRoomId",
                table: "MeetingRooms",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "CalendarRoomId",
                table: "MeetingRooms",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CalendarRoomId",
                table: "MeetingRooms",
                column: "CalendarRoomId",
                filter: "\"CalendarRoomId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CameraRoomId",
                table: "MeetingRooms",
                column: "CameraRoomId",
                unique: true,
                filter: "\"CameraRoomId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_CalendarRoomId",
                table: "MeetingRooms");

            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_CameraRoomId",
                table: "MeetingRooms");

            migrationBuilder.AlterColumn<string>(
                name: "CameraRoomId",
                table: "MeetingRooms",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalendarRoomId",
                table: "MeetingRooms",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CalendarRoomId",
                table: "MeetingRooms",
                column: "CalendarRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CameraRoomId",
                table: "MeetingRooms",
                column: "CameraRoomId",
                unique: true);
        }
    }
}
