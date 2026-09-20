using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Models;

namespace PlantCare.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<PlantProfile> PlantProfiles => Set<PlantProfile>();

    public DbSet<CareTask> CareTasks => Set<CareTask>();

    public DbSet<CareTaskLog> CareTaskLogs => Set<CareTaskLog>();

    public DbSet<PlantNote> PlantNotes => Set<PlantNote>();

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlantProfile>(entity =>
        {
            entity.Property(p => p.LightRequirement).HasConversion<string>();
            entity.HasIndex(p => p.CommonName).IsUnique();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(r => r.Orientation).HasConversion<string>();
            entity.Property(r => r.LightExposure).HasConversion<string>();
            entity.Property(r => r.Humidity).HasConversion<string>();
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Plant>(entity =>
        {
            entity.HasOne(p => p.PlantProfile)
                .WithMany(pp => pp.Plants)
                .HasForeignKey(p => p.PlantProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Room)
                .WithMany(r => r.Plants)
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CareTask>(entity =>
        {
            entity.Property(t => t.Type).HasConversion<string>();

            entity.HasOne(t => t.Plant)
                .WithMany(p => p.CareTasks)
                .HasForeignKey(t => t.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => new { t.PlantId, t.Type }).IsUnique();
        });

        modelBuilder.Entity<PlantNote>(entity =>
        {
            entity.HasOne(n => n.Plant)
                .WithMany()
                .HasForeignKey(n => n.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.PlantId, n.CreatedAt });
        });

        modelBuilder.Entity<CareTaskLog>(entity =>
        {
            entity.HasOne(l => l.CareTask)
                .WithMany(t => t.Logs)
                .HasForeignKey(l => l.CareTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(l => l.Method).HasConversion<string>();

            entity.HasIndex(l => new { l.CareTaskId, l.DoneAt });
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.Property(n => n.Type).HasConversion<string>();

            entity.HasOne(n => n.Plant)
                .WithMany(p => p.NotificationLogs)
                .HasForeignKey(n => n.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.PlantId, n.Type, n.SentAt });
        });
    }
}
