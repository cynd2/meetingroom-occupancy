using Microsoft.EntityFrameworkCore;
using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<MeetingRoom> MeetingRooms { get; set; } = null!;

    public DbSet<RoomSnapshot> CameraDetections { get; set; } = null!;

    public DbSet<Booking> CalendarBookings { get; set; } = null!;

    public DbSet<DetectionResult> DetectionResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeetingRoom>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.CameraRoomId).IsUnique().HasFilter("\"CameraRoomId\" IS NOT NULL");
            entity.HasIndex(m => m.CalendarRoomId).HasFilter("\"CalendarRoomId\" IS NOT NULL");
            entity.HasOne(m => m.CurrentBooking)
                  .WithMany()
                  .HasForeignKey(m => m.CurrentBookingEventId)
                  .HasPrincipalKey(b => b.EventId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RoomSnapshot>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasIndex(c => new { c.RoomId, c.CapturedAt });
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.EventId);

            entity.HasIndex(b => new { b.RoomId, b.StartTime, b.EndTime });

            entity.HasIndex(b => b.Status);
        });

        modelBuilder.Entity<DetectionResult>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasIndex(d => new { d.RoomId, d.EvaluatedAt });

            entity.HasIndex(d => new { d.Status, d.AlertSent });
        });
    }
}