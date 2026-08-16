using System.Security.Claims;
using Astraea.Application;
using Astraea.Domain;
using Astraea.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner,Mentor")]
[Route("api/refresher-content")]
public sealed class RefresherContentController(AstraeaDbContext db) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("skills/{skillId:guid}")]
    public async Task<IReadOnlyCollection<RefresherContentDto>> GetForSkill(Guid skillId, CancellationToken ct)
    {
        var skill = await db.Skills
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == skillId && !x.IsArchived, ct)
            ?? throw new KeyNotFoundException("Skill not found.");

        var ownsSkill = skill.LearnerId == Me;
        var mentorsLearner = await db.MentorLearners
            .AsNoTracking()
            .AnyAsync(x => x.MentorUserId == Me && x.LearnerId == skill.LearnerId && x.Status == MentorLearnerStatus.Accepted, ct);

        if (!ownsSkill && !mentorsLearner)
        {
            throw new UnauthorizedAccessException("You cannot view refresher content for this skill.");
        }

        var saved = await db.RefresherContents
            .AsNoTracking()
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.Title)
            .Select(x => new RefresherContentDto(x.Id, x.SkillId, x.Title, x.Url, x.Provider))
            .ToListAsync(ct);

        if (saved.Count > 0)
        {
            return saved;
        }

        return
        [
            Generated(skill.Id, $"{skill.Title} refresher for self taught learners", $"{skill.Title} refresher self taught learners"),
            Generated(skill.Id, $"{skill.Title} practice review", $"{skill.Title} practice review tutorial")
        ];
    }

    private static RefresherContentDto Generated(Guid skillId, string title, string query)
    {
        return new RefresherContentDto(Guid.Empty, skillId, title, "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query), "YouTube");
    }
}
