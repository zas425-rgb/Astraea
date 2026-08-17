using System.Security.Claims;
using Astraea.Application;
using Astraea.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner,Mentor,Both")]
[Route("api/mentor-invitations")]
public sealed class MentorInvitationController(IMentorService mentors, IReminderService reminders) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IReadOnlyCollection<MentorLearnerVm>> Get(CancellationToken ct)
    {
        var invitations = await mentors.GetPendingInvitationsForMentorAsync(Me, ct);
        return invitations.Select(x => x.ToVm()).ToArray();
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<AuthResponseVm> Accept(Guid id, CancellationToken ct)
    {
        var response = await mentors.AcceptInvitationAsync(id, Me, ct);
        return response.ToVm();
    }

    [HttpPost("{id:guid}/decline")]
    public Task Decline(Guid id, CancellationToken ct)
    {
        return mentors.DeclineInvitationAsync(id, Me, ct);
    }

    [HttpGet("mentees")]
    public async Task<IReadOnlyCollection<LearnerSummaryVm>> Mentees(CancellationToken ct)
    {
        var mentees = await mentors.GetActiveMenteesAsync(Me, ct);
        return mentees.Select(x => x.ToVm()).ToArray();
    }

    [HttpGet("learners/{learnerId:guid}")]
    public async Task<LearnerDashboardVm> Dashboard(Guid learnerId, CancellationToken ct)
    {
        var dashboard = await mentors.GetLearnerDashboardReadOnlyAsync(Me, learnerId, ct);
        return dashboard.ToVm();
    }

    [HttpPost("learners/{learnerId:guid}/fading-reminder")]
    public Task Reminder(Guid learnerId, CancellationToken ct)
    {
        return reminders.SendFadingSkillReminderAsync(Me, learnerId, ct);
    }
}
