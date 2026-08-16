using Astraea.Application;
using Astraea.Application.Abstractions;
using Astraea.Domain;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class ReminderService(AstraeaDbContext db, IUnitOfWork uow) : IReminderService
{
    public async Task SendFadingSkillReminderAsync(Guid mentorId, Guid learnerId, CancellationToken ct)
    {
        var mentor = await db.Users.SingleOrDefaultAsync(x => x.Id == mentorId, ct) ?? throw new UnauthorizedAccessException("Mentor account not found.");
        var connection = await db.MentorLearners
            .SingleOrDefaultAsync(x =>
                x.LearnerId == learnerId &&
                x.Status == MentorLearnerStatus.Accepted &&
                (x.MentorUserId == mentorId || x.MentorEmail == mentor.Email), ct)
            ?? throw new UnauthorizedAccessException("You do not mentor this learner.");

        if (connection.MentorUserId != mentorId)
        {
            connection.MentorUserId = mentorId;
            connection.StatusUpdatedAtUtc = DateTime.UtcNow;
        }

        var now = DateTime.UtcNow;
        var skills = await db.Skills.Where(x => x.LearnerId == learnerId && !x.IsArchived).ToListAsync(ct);
        var skill = skills
            .OrderByDescending(x => (now - x.LastReviewedUtc).TotalDays / Math.Max(x.CurrentIntervalDays, 1))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("This learner has no active skills.");

        var retention = 100 * Math.Exp(-Math.Max(0, (now - skill.LastReviewedUtc).TotalDays) / Math.Max(skill.CurrentIntervalDays, 1));
        var message = retention < 70
            ? $"{mentor.FullName} noticed that {skill.Title} needs a refresher."
            : $"{mentor.FullName} sent a check-in for {skill.Title}, your next best skill to keep bright.";

        db.Notifications.Add(new Notification
        {
            LearnerId = learnerId,
            MentorId = mentorId,
            SkillId = skill.Id,
            Message = message
        });
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<MentorReminderDto>> GetUnreadForLearnerAsync(Guid learnerId, CancellationToken ct)
    {
        return await db.Notifications
            .AsNoTracking()
            .Where(x => x.LearnerId == learnerId && !x.IsRead)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MentorReminderDto(x.Id, x.Mentor.FullName, x.Skill.Title, x.Message, x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid learnerId, Guid notificationId, CancellationToken ct)
    {
        var item = await db.Notifications
            .SingleOrDefaultAsync(x => x.Id == notificationId && x.LearnerId == learnerId && !x.IsRead, ct)
            ?? throw new KeyNotFoundException("Reminder not found.");

        item.IsRead = true;
        item.ReadAtUtc = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
    }
}
