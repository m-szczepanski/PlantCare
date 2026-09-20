using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum JournalWriteStatus
{
    Success,
    PlantNotFound,
    Empty,
}

public sealed record JournalWriteResult(JournalWriteStatus Status, JournalEntryResponseDto? Entry = null);

public interface IJournalService
{
    Task<IReadOnlyList<JournalEntryResponseDto>?> ListAsync(int plantId, CancellationToken cancellationToken = default);

    Task<JournalWriteResult> CreateAsync(
        int plantId,
        DateTime? entryDate,
        string? text,
        Stream? photo,
        string? photoContentType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int plantId, int entryId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dated photo + note growth journal per plant; entries render newest-first and
/// feed the before/after comparison on the detail page.
/// </summary>
public sealed class JournalService(AppDbContext db, IPlantPhotoStorage photos) : IJournalService
{
    public async Task<IReadOnlyList<JournalEntryResponseDto>?> ListAsync(int plantId, CancellationToken cancellationToken = default)
    {
        if (!await db.Plants.AnyAsync(p => p.Id == plantId, cancellationToken))
        {
            return null;
        }

        return await db.JournalEntries
            .AsNoTracking()
            .Where(j => j.PlantId == plantId)
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.Id)
            .Select(j => new JournalEntryResponseDto
            {
                Id = j.Id,
                EntryDate = j.EntryDate,
                PhotoUrl = j.PhotoUrl,
                Text = j.Text,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<JournalWriteResult> CreateAsync(
        int plantId,
        DateTime? entryDate,
        string? text,
        Stream? photo,
        string? photoContentType,
        CancellationToken cancellationToken = default)
    {
        var trimmed = text?.Trim();
        if (photo is null && string.IsNullOrEmpty(trimmed))
        {
            return new JournalWriteResult(JournalWriteStatus.Empty);
        }

        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == plantId, cancellationToken);
        if (plant is null)
        {
            return new JournalWriteResult(JournalWriteStatus.PlantNotFound);
        }

        string? photoUrl = null;
        if (photo is not null)
        {
            try
            {
                photoUrl = await photos.SaveAsync(plantId, photo, photoContentType ?? string.Empty, "journal", cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidDataException or PhotoStorageException)
            {
                return new JournalWriteResult(JournalWriteStatus.Empty);
            }
        }

        var entry = new JournalEntry
        {
            PlantId = plantId,
            EntryDate = (entryDate ?? DateTime.UtcNow).Date.Add(
                entryDate is null ? DateTime.UtcNow.TimeOfDay : TimeOnly.MinValue.ToTimeSpan()),
            PhotoUrl = photoUrl,
            Text = string.IsNullOrEmpty(trimmed) ? null : trimmed,
        };

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return new JournalWriteResult(JournalWriteStatus.Success, new JournalEntryResponseDto
        {
            Id = entry.Id,
            EntryDate = entry.EntryDate,
            PhotoUrl = entry.PhotoUrl,
            Text = entry.Text,
        });
    }

    public async Task<bool> DeleteAsync(int plantId, int entryId, CancellationToken cancellationToken = default)
    {
        var entry = await db.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == entryId && j.PlantId == plantId, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        photos.DeleteIfManaged(entry.PhotoUrl);
        db.JournalEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
