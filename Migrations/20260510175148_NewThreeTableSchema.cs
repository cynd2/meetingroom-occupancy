using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingRoomSystem.Migrations
{
    /// <inheritdoc />
    public partial class NewThreeTableSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhostBookings");

            migrationBuilder.DropTable(
                name: "SquatterIncidents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomSnapshots",
                table: "RoomSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookings",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "RoomSnapshots",
                newName: "CameraDetections");

            migrationBuilder.RenameTable(
                name: "Bookings",
                newName: "CalendarBookings");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "CameraDetections",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FramePath",
                table: "CameraDetections",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoomId",
                table: "CameraDetections",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CalendarBookings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RoomId",
                table: "CalendarBookings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CameraDetections",
                table: "CameraDetections",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CalendarBookings",
                table: "CalendarBookings",
                column: "EventId");

            migrationBuilder.CreateTable(
                name: "DetectionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<string>(type: "TEXT", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    AnomalyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AlertSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlertSentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CameraDetections_RoomId_CapturedAt",
                table: "CameraDetections",
                columns: new[] { "RoomId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_RoomId_StartTime_EndTime",
                table: "CalendarBookings",
                columns: new[] { "RoomId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBookings_Status",
                table: "CalendarBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DetectionResults_RoomId_EvaluatedAt",
                table: "DetectionResults",
                columns: new[] { "RoomId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DetectionResults_Status_AlertSent",
                table: "DetectionResults",
                columns: new[] { "Status", "AlertSent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectionResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CameraDetections",
                table: "CameraDetections");

            migrationBuilder.DropIndex(
                name: "IX_CameraDetections_RoomId_CapturedAt",
                table: "CameraDetections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CalendarBookings",
                table: "CalendarBookings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_RoomId_StartTime_EndTime",
                table: "CalendarBookings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBookings_Status",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "CameraDetections");

            migrationBuilder.DropColumn(
                name: "FramePath",
                table: "CameraDetections");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "CameraDetections");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "CalendarBookings");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "CalendarBookings");

            migrationBuilder.RenameTable(
                name: "CameraDetections",
                newName: "RoomSnapshots");

            migrationBuilder.RenameTable(
                name: "CalendarBookings",
                newName: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomSnapshots",
                table: "RoomSnapshots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookings",
                table: "Bookings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "GhostBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlertSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlertSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BookingEndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BookingStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmptyForMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizerEmail = table.Column<string>(type: "TEXT", nullable: false),
                    RoomName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SquatterIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OccupiedForMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RoomName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SquatterIncidents", x => x.Id);
                });
        }
    }
}
