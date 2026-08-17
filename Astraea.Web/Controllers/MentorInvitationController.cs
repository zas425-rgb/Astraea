using System.Security.Claims;
using Astraea.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner,Mentor,Both")]
[Route("api/mentor-invitations")]
public sealed class MentorInvitationController(IMentorService mentors, IReminderService reminders) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<IReadOnlyCollection<MentorLearnerDto>> Get(CancellationToken ct) => mentors.GetPendingInvitationsForMentorAsync(Me, ct);
    [HttpPost("{id:guid}/accept")] public Task<AuthResponseDto> Accept(Guid id, CancellationToken ct) => mentors.AcceptInvitationAsync(id, Me, ct);
    [HttpPost("{id:guid}/decline")] public Task Decline(Guid id, CancellationToken ct) => mentors.DeclineInvitationAsync(id, Me, ct);
    [HttpGet("mentees")] public Task<IReadOnlyCollection<LearnerSummaryDto>> Mentees(CancellationToken ct) => mentors.GetActiveMenteesAsync(Me, ct);
    [HttpGet("learners/{learnerId:guid}")] public Task<LearnerDashboardDto> Dashboard(Guid learnerId, CancellationToken ct) => mentors.GetLearnerDashboardReadOnlyAsync(Me, learnerId, ct);
    [HttpPost("learners/{learnerId:guid}/fading-reminder")] public Task Reminder(Guid learnerId, CancellationToken ct) => reminders.SendFadingSkillReminderAsync(Me, learnerId, ct);
}
