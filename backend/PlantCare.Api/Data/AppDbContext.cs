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

    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    public DbSet<JobRunLog> JobRuns => Set<JobRunLog>();

    public DbSet<NotificationDigest> NotificationDigests => Set<NotificationDigest>();

    public DbSet<PlantProfileTranslation> PlantProfileTranslations => Set<PlantProfileTranslation>();

    public DbSet<SoilMix> SoilMixes => Set<SoilMix>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlantProfile>(entity =>
        {
            entity.Property(p => p.LightRequirement).HasConversion<string>();
            entity.HasIndex(p => p.CommonName).IsUnique();
        });

        modelBuilder.Entity<SoilMix>(entity =>
        {
            entity.HasIndex(m => m.Name).IsUnique();
        });

        modelBuilder.Entity<PlantProfileTranslation>(entity =>
        {
            entity.HasOne(t => t.PlantProfile)
                .WithMany(p => p.Translations)
                .HasForeignKey(t => t.PlantProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => new { t.PlantProfileId, t.Language }).IsUnique();
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
            entity.Property(p => p.SoilType).HasConversion<string>();

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

        modelBuilder.Entity<JobRunLog>(entity =>
        {
            entity.HasIndex(j => j.RanAt);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasOne(j => j.Plant)
                .WithMany()
                .HasForeignKey(j => j.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(j => new { j.PlantId, j.EntryDate });
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

        modelBuilder.Entity<NotificationDigest>(entity =>
        {
            entity.HasIndex(d => d.SentAt);
        });
    }
}
