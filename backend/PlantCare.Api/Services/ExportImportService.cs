using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public interface IExportImportService
{
    Task<ExportDocumentDto> ExportAsync(CancellationToken cancellationToken = default);

    Task<ImportResultDto> ImportAsync(ExportDocumentDto document, CancellationToken cancellationToken = default);
}

/// <summary>
/// Whole-database JSON snapshot and merge-import keyed on natural names
/// (room name, profile common name, plant nickname). Uploaded photo FILES
/// are not part of the export — only their /uploads paths; restoring onto an
/// instance with the same volume keeps them, a fresh one shows placeholders.
/// </summary>
public sealed class ExportImportService(AppDbContext db) : IExportImportService
{
    public const int CurrentSchemaVersion = 1;

    public async Task<ExportDocumentDto> ExportAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await db.Rooms.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var profiles = await db.PlantProfiles.AsNoTracking().OrderBy(p => p.CommonName).ToListAsync(cancellationToken);
        var plants = await db.Plants
            .AsNoTracking()
            .Include(p => p.Room)
            .Include(p => p.PlantProfile)
            .Include(p => p.CareTasks)
            .ThenInclude(t => t.Logs)
            .OrderBy(p => p.NickName)
            .ToListAsync(cancellationToken);
        var notes = await db.PlantNotes.AsNoTracking().ToListAsync(cancellationToken);
        var journal = await db.JournalEntries.AsNoTracking().ToListAsync(cancellationToken);

        return new ExportDocumentDto
        {
            SchemaVersion = CurrentSchemaVersion,
            ExportedAt = DateTime.UtcNow,
            Rooms = rooms.Select(r => new ExportRoomDto
            {
                Name = r.Name,
                Orientation = r.Orientation,
                LightExposure = r.LightExposure,
                Humidity = r.Humidity,
                TemperatureCelsius = r.TemperatureCelsius,
            }).ToList(),
            PlantProfiles = profiles.Select(p => new ExportProfileDto
            {
                CommonName = p.CommonName,
                ScientificName = p.ScientificName,
                DefaultWateringIntervalDays = p.DefaultWateringIntervalDays,
                LightRequirement = p.LightRequirement,
                HumidityNotes = p.HumidityNotes,
                CareTips = p.CareTips,
                DefaultReduceInWinter = p.DefaultReduceInWinter,
                ToxicToPets = p.ToxicToPets,
                ToxicToChildren = p.ToxicToChildren,
                DiagnosisChecklist = p.DiagnosisChecklist,
            }).ToList(),
            Plants = plants.Select(p => new ExportPlantDto
            {
                NickName = p.NickName,
                RoomName = p.Room?.Name,
                ProfileCommonName = p.PlantProfile?.CommonName,
                AcquiredDate = p.AcquiredDate,
                PhotoUrl = p.PhotoUrl,
                PotSizeCm = p.PotSizeCm,
                SoilMix = p.SoilMix,
                PropagatedFrom = p.PropagatedFrom,
                NotifyEnabled = p.NotifyEnabled,
                SnoozedUntil = p.SnoozedUntil,
                CareTasks = p.CareTasks.OrderBy(t => t.Type).Select(t => new ExportCareTaskDto
                {
                    Type = t.Type,
                    IntervalDays = t.IntervalDays,
                    LastDoneAt = t.LastDoneAt,
                    ReduceInWinter = t.ReduceInWinter,
                    Logs = t.Logs.OrderBy(l => l.DoneAt).Select(l => new ExportCareTaskLogDto
                    {
                        DoneAt = l.DoneAt,
                        Note = l.Note,
                        AmountMilliliters = l.AmountMilliliters,
                        Method = l.Method,
                    }).ToList(),
                }).ToList(),
                Notes = notes.Where(n => n.PlantId == p.Id).OrderBy(n => n.CreatedAt).Select(n => new ExportNoteDto
                {
                    CreatedAt = n.CreatedAt,
                    Text = n.Text,
                }).ToList(),
                Journal = journal.Where(j => j.PlantId == p.Id).OrderBy(j => j.EntryDate).Select(j => new ExportJournalEntryDto
                {
                    EntryDate = j.EntryDate,
                    PhotoUrl = j.PhotoUrl,
                    Text = j.Text,
                }).ToList(),
            }).ToList(),
        };
    }

    public async Task<ImportResultDto> ImportAsync(ExportDocumentDto document, CancellationToken cancellationToken = default)
    {
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Unsupported export schema version {document.SchemaVersion}.");
        }

        var roomsCreated = 0;
        var roomByName = await db.Rooms.ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var room in document.Rooms)
        {
            if (roomByName.ContainsKey(room.Name))
            {
                continue;
            }

            var entity = new Room
            {
                Name = room.Name,
                Orientation = room.Orientation,
                LightExposure = room.LightExposure,
                Humidity = room.Humidity,
                TemperatureCelsius = room.TemperatureCelsius,
            };
            db.Rooms.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            roomByName[room.Name] = entity;
            roomsCreated++;
        }

        var profilesCreated = 0;
        var profileByName = await db.PlantProfiles.ToDictionaryAsync(p => p.CommonName, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var profile in document.PlantProfiles)
        {
            if (profileByName.ContainsKey(profile.CommonName))
            {
                continue;
            }

            var entity = new PlantProfile
            {
                CommonName = profile.CommonName,
                ScientificName = profile.ScientificName,
                DefaultWateringIntervalDays = profile.DefaultWateringIntervalDays,
                LightRequirement = profile.LightRequirement,
                HumidityNotes = profile.HumidityNotes,
                CareTips = profile.CareTips,
                DefaultReduceInWinter = profile.DefaultReduceInWinter,
                ToxicToPets = profile.ToxicToPets,
                ToxicToChildren = profile.ToxicToChildren,
                DiagnosisChecklist = profile.DiagnosisChecklist,
            };
            db.PlantProfiles.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            profileByName[profile.CommonName] = entity;
            profilesCreated++;
        }

        var existingNicknames = (await db.Plants.Select(p => p.NickName).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plantsCreated = 0;
        var plantsSkipped = 0;

        foreach (var plant in document.Plants)
        {
            if (!existingNicknames.Add(plant.NickName))
            {
                plantsSkipped++;
                continue;
            }

            var entity = new Plant
            {
                NickName = plant.NickName,
                RoomId = plant.RoomName is not null && roomByName.TryGetValue(plant.RoomName, out var room) ? room.Id : null,
                PlantProfileId = plant.ProfileCommonName is not null && profileByName.TryGetValue(plant.ProfileCommonName, out var profile) ? profile.Id : null,
                AcquiredDate = plant.AcquiredDate,
                PhotoUrl = plant.PhotoUrl,
                PotSizeCm = plant.PotSizeCm,
                SoilMix = plant.SoilMix,
                PropagatedFrom = plant.PropagatedFrom,
                NotifyEnabled = plant.NotifyEnabled,
                SnoozedUntil = plant.SnoozedUntil,
            };

            foreach (var task in plant.CareTasks)
            {
                entity.CareTasks.Add(new CareTask
                {
                    Type = task.Type,
                    IntervalDays = task.IntervalDays,
                    LastDoneAt = task.LastDoneAt,
                    ReduceInWinter = task.ReduceInWinter,
                    Logs = task.Logs.Select(l => new CareTaskLog
                    {
                        DoneAt = l.DoneAt,
                        Note = l.Note,
                        AmountMilliliters = l.AmountMilliliters,
                        Method = l.Method,
                    }).ToList(),
                });
            }

            foreach (var note in plant.Notes)
            {
                db.PlantNotes.Add(new PlantNote { Plant = entity, CreatedAt = note.CreatedAt, Text = note.Text });
            }

            foreach (var entry in plant.Journal)
            {
                db.JournalEntries.Add(new JournalEntry { Plant = entity, EntryDate = entry.EntryDate, PhotoUrl = entry.PhotoUrl, Text = entry.Text });
            }

            db.Plants.Add(entity);
            plantsCreated++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ImportResultDto
        {
            RoomsCreated = roomsCreated,
            ProfilesCreated = profilesCreated,
            PlantsCreated = plantsCreated,
            PlantsSkipped = plantsSkipped,
        };
    }
}
