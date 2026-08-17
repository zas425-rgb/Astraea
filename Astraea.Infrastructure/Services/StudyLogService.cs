using Astraea.Application;
using Astraea.Application.Abstractions;
using Astraea.Application.Retention;
using Astraea.Application.Study;
using Astraea.Domain;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class StudyLogService(
    AstraeaDbContext db,
    IUnitOfWork uow,
    IRetentionService retention) : IStudyLogService
{
    public async Task<CelestialNodeDto> RecordAsync(
        Guid learnerId,
        StudyLogCreateDto request,
        CancellationToken ct)
    {
        if (request.SelfRating is < 1 or > 5)
        {
            throw new InvalidOperationException("Self-rating must be between 1 and 5.");
        }

        var skill = await db.Skills
            .Include(x => x.Prerequisites)
            .SingleOrDefaultAsync(
                x => x.Id == request.SkillId
                    && x.LearnerId == learnerId
                    && !x.IsArchived,
                ct)
            ?? throw new KeyNotFoundException("Skill not found.");

        var (easeFactor, intervalDays) = retention.ApplyReview(
            skill.EaseFactor,
            skill.CurrentIntervalDays,
            request.SelfRating);

        skill.EaseFactor = easeFactor;
        skill.CurrentIntervalDays = intervalDays;
        skill.LastReviewedUtc = request.StudiedAtUtc.ToUniversalTime();
        skill.NextReviewDueDateUtc = skill.LastReviewedUtc.AddDays(intervalDays);

        db.StudyLogs.Add(new StudyLog
        {
            SkillId = skill.Id,
            StudiedAtUtc = skill.LastReviewedUtc,
            SelfRating = request.SelfRating,
            Notes = request.Notes.Trim()
        });

        await uow.SaveChangesAsync(ct);
        return SkillService.Map(skill);
    }
}
