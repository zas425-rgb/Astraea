using System.Security.Claims;
using Astraea.Application.Abstractions;
using Astraea.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/skills")]
public sealed class SkillLifecycleController(AstraeaDbContext db, IUnitOfWork uow) : ControllerBase
{
    private Guid LearnerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:guid}/archive")]
    public Task Archive(Guid id, CancellationToken ct) => SetArchiveAsync(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public Task Restore(Guid id, CancellationToken ct) => SetArchiveAsync(id, false, ct);

    [HttpGet("archived")]
    public async Task<IReadOnlyCollection<ArchivedSkillDto>> Archived(CancellationToken ct)
    {
        var skills = await db.Skills.AsNoTracking().Where(x => x.LearnerId == LearnerId && x.IsArchived).OrderBy(x => x.Title).ToListAsync(ct);
        var now = DateTime.UtcNow;
        return skills.Select(x => new ArchivedSkillDto(x.Id, x.Title, x.ConstellationCategory, x.EaseFactor, x.CurrentIntervalDays, x.CanvasX, x.CanvasY, Math.Round(Math.Clamp(100 * Math.Exp(-Math.Max(0, (now - x.LastReviewedUtc).TotalDays) / Math.Max(x.CurrentIntervalDays, 1)), 0, 100), 1))).ToArray();
    }

    [HttpDelete("archived")]
    public async Task ClearArchived(CancellationToken ct)
    {
        var skills = await db.Skills.Where(x => x.LearnerId == LearnerId && x.IsArchived).ToListAsync(ct);
        var ids = skills.Select(x => x.Id).ToArray();
        db.SkillPrerequisites.RemoveRange(db.SkillPrerequisites.Where(x => ids.Contains(x.SkillId) || ids.Contains(x.PrerequisiteSkillId)));
        db.Notifications.RemoveRange(db.Notifications.Where(x => ids.Contains(x.SkillId)));
        await uow.SaveChangesAsync(ct);
        db.Skills.RemoveRange(skills);
        await uow.SaveChangesAsync(ct);
    }

    private async Task SetArchiveAsync(Guid id, bool archived, CancellationToken ct)
    {
        var skill = await db.Skills.SingleOrDefaultAsync(x => x.Id == id && x.LearnerId == LearnerId, ct) ?? throw new KeyNotFoundException("Skill not found.");
        skill.IsArchived = archived;
        await uow.SaveChangesAsync(ct);
    }
}

public record ArchivedSkillDto(Guid Id, string Title, string ConstellationCategory, double EaseFactor, int CurrentIntervalDays, double CanvasX, double CanvasY, double RetentionPercent);
