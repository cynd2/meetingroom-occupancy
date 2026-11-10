using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingRoomSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingRoomsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    CameraRoomId = table.Column<string>(type: "TEXT", nullable: false),
                    IsOccupied = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CalendarRoomId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentBookingEventId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingRooms_CalendarBookings_CurrentBookingEventId",
                        column: x => x.CurrentBookingEventId,
                        principalTable: "CalendarBookings",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CalendarRoomId",
                table: "MeetingRooms",
                column: "CalendarRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CameraRoomId",
                table: "MeetingRooms",
                column: "CameraRoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CurrentBookingEventId",
                table: "MeetingRooms",
                column: "CurrentBookingEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingRooms");
        }
    }
}
