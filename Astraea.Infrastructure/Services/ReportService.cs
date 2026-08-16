using Astraea.Application;
using Astraea.Application.Retention;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class ReportService(AstraeaDbContext db, IRetentionService retention) : IReportService
{
    public async Task<LearnerReportDto> GetLearnerReportAsync(Guid learnerId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var skills = await db.Skills.AsNoTracking().Where(x => x.LearnerId == learnerId && !x.IsArchived).ToListAsync(ct);
        var rows = skills.Select(skill =>
        {
            var current = retention.CalculateRetention(skill.LastReviewedUtc, skill.CurrentIntervalDays, now);
            var previous = retention.CalculateRetention(skill.LastReviewedUtc, skill.CurrentIntervalDays, now.AddDays(-30));
            return new SkillReportRowDto(skill.Title, skill.ConstellationCategory, Math.Round(current, 1), Math.Round(current - previous, 1), retention.GetRiskStatus(current).ToString());
        }).OrderBy(x => x.Skill).ToArray();
        var reviewDays = await db.StudyLogs.AsNoTracking().Where(x => x.Skill.LearnerId == learnerId && x.StudiedAtUtc >= monthStart).Select(x => x.StudiedAtUtc.Date).Distinct().OrderBy(x => x).ToListAsync(ct);
        var longestStreak = 0; var streak = 0; DateTime? previousDay = null;
        foreach (var day in reviewDays) { streak = previousDay?.AddDays(1) == day ? streak + 1 : 1; longestStreak = Math.Max(longestStreak, streak); previousDay = day; }
        return new LearnerReportDto(rows, reviewDays.Count, longestStreak, rows.Length == 0 ? 0 : Math.Round(rows.Average(x => x.ThirtyDayChangePercent), 1));
    }
}
