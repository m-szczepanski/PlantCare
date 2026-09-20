using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public interface IInsightsService
{
    Task<InsightsResponseDto> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only aggregate statistics over the whole collection. Single-user dataset:
/// the queries run over the full plant list and the last year of logs in memory.
/// </summary>
public sealed class InsightsService(AppDbContext db) : IInsightsService
{
    private const int AdherenceWindowDays = 30;
    private const int MonthWindow = 12;

    public async Task<InsightsResponseDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var plants = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.CareTasks)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var wateringTasks = plants
            .ToDictionary(
                p => p.Id,
                p => p.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering));
        var wateringTaskIds = wateringTasks.Values
            .Where(t => t is not null)
            .ToDictionary(t => t!.Id, t => t!.PlantId);

        var logs = await db.CareTaskLogs
            .AsNoTracking()
            .Where(w => w.DoneAt >= now.AddMonths(-MonthWindow) && w.CareTask.Type == CareTaskType.Watering)
            .Select(w => new { w.CareTaskId, w.DoneAt })
            .ToListAsync(cancellationToken);

        var logsWithPlants = logs
            .Where(l => wateringTaskIds.ContainsKey(l.CareTaskId))
            .Select(l => new { PlantId = wateringTaskIds[l.CareTaskId], WateredAt = l.DoneAt })
            .ToList();

        var intervalByPlant = plants.ToDictionary(
            p => p.Id,
            p => wateringTasks[p.Id]?.IntervalDays ?? p.PlantProfile?.DefaultWateringIntervalDays);
        var lastWateredByPlant = plants.ToDictionary(
            p => p.Id,
            p => wateringTasks[p.Id]?.LastDoneAt);
        var scheduled = plants
            .Where(p => intervalByPlant[p.Id] is int days && days >= 1)
            .ToList();

        var species = plants
            .Where(p => p.PlantProfile is not null)
            .GroupBy(p => p.PlantProfile!.Id)
            .Select(g => new SpeciesCountDto
            {
                PlantProfileId = g.Key,
                CommonName = g.First().PlantProfile!.CommonName,
                PlantCount = g.Count(),
            })
            .OrderByDescending(s => s.PlantCount)
            .ThenBy(s => s.CommonName, StringComparer.Ordinal)
            .ToList();

        var mostNeglected = plants
            .Select(p => new NeglectedPlantDto
            {
                PlantId = p.Id,
                NickName = p.NickName,
                DaysSinceLastWatering = (int)(now - (lastWateredByPlant[p.Id] ?? p.AcquiredDate)).TotalDays,
            })
            .OrderByDescending(n => n.DaysSinceLastWatering)
            .Take(5)
            .ToList();

        var scheduledIds = scheduled.Select(p => p.Id).ToHashSet();
        var windowStart = now.AddDays(-AdherenceWindowDays);
        var expected = (int)Math.Round(scheduled.Sum(p =>
            (double)AdherenceWindowDays / intervalByPlant[p.Id]!.Value));
        var actual = logsWithPlants.Count(w => scheduledIds.Contains(w.PlantId) && w.WateredAt >= windowStart);

        var streaks = scheduled
            .Select(p => new StreakDto
            {
                PlantId = p.Id,
                NickName = p.NickName,
                ConsecutiveOnTimeWaterings = Streak(p.Id, intervalByPlant[p.Id]!.Value),
            })
            .Where(s => s.ConsecutiveOnTimeWaterings > 0)
            .OrderByDescending(s => s.ConsecutiveOnTimeWaterings)
            .Take(5)
            .ToList();

        var monthly = new List<MonthlyCountDto>();
        for (var back = MonthWindow - 1; back >= 0; back -= 1)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-back);
            var key = month.ToString("yyyy-MM");
            monthly.Add(new MonthlyCountDto
            {
                Month = key,
                Count = logsWithPlants.Count(w => w.WateredAt.ToString("yyyy-MM") == key),
            });
        }

        return new InsightsResponseDto
        {
            TotalPlants = plants.Count,
            ScheduledPlants = scheduled.Count,
            UnscheduledPlants = plants.Count - scheduled.Count,
            SpeciesCount = species.Count,
            Species = species,
            MostNeglected = mostNeglected,
            AdherenceWindowDays = AdherenceWindowDays,
            AdherencePercent = expected == 0 ? 0 : Math.Clamp((double)actual / expected * 100, 0, 100),
            ExpectedWateringsInWindow = expected,
            ActualWateringsInWindow = actual,
            Streaks = streaks,
            MonthlyWaterings = monthly,
        };

        int Streak(int plantId, int intervalDays)
        {
            var plantLogs = logsWithPlants
                .Where(w => w.PlantId == plantId)
                .OrderByDescending(w => w.WateredAt)
                .ToList();

            if (plantLogs.Count == 0)
            {
                return 0;
            }

            var streak = 1;
            for (var i = 1; i < plantLogs.Count; i += 1)
            {
                var gapDays = (plantLogs[i - 1].WateredAt - plantLogs[i].WateredAt).TotalDays;
                if (gapDays > intervalDays + 2)
                {
                    break;
                }
                streak += 1;
            }
            return streak;
        }
    }
}
