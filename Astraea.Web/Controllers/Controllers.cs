using System.Security.Claims;
using Astraea.Application;
using Astraea.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astraea.Web;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<AuthResponseVm> Register(RegisterUserRequest request, CancellationToken ct)
    {
        var response = await service.RegisterAsync(request, ct);
        return response.ToVm();
    }

    [HttpPost("login")]
    public async Task<AuthResponseVm> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await service.LoginAsync(request, ct);
        return response.ToVm();
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetForgottenPassword(
        ForgotPasswordResetRequest request,
        CancellationToken ct)
    {
        try
        {
            await service.ResetForgottenPasswordAsync(request, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public Task ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        return service.ChangePasswordAsync(CurrentUserId, request, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/learner/mentors")]
public sealed class LearnerMentorsController(IMentorService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<MentorLearnerVm>> Get(CancellationToken ct)
    {
        var mentors = await service.GetLearnerMentorsAsync(CurrentUserId, ct);
        return mentors.Select(x => x.ToVm()).ToArray();
    }

    [HttpPost("invite")]
    public async Task<ActionResult<MentorLearnerVm>> Invite(
        MentorInviteRequest request,
        CancellationToken ct)
    {
        try
        {
            var invite = await service.InviteMentorAsync(CurrentUserId, request.Email, ct);
            return Ok(invite.ToVm());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("invitations/{id:guid}")]
    public Task Cancel(Guid id, CancellationToken ct)
    {
        return service.CancelInvitationAsync(id, CurrentUserId, ct);
    }

    [HttpPost("{id:guid}/revoke")]
    public Task Revoke(Guid id, CancellationToken ct)
    {
        return service.RevokeAccessAsync(id, CurrentUserId, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/skills")]
public sealed class SkillsController(ISkillService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<CelestialNodeVm>> Get(CancellationToken ct)
    {
        var skills = await service.GetSkillsAsync(CurrentUserId, ct);
        return skills.Select(x => x.ToVm()).ToArray();
    }

    [HttpPost]
    public async Task<CelestialNodeVm> Create(CreateSkillRequest request, CancellationToken ct)
    {
        var skill = await service.CreateSkillAsync(CurrentUserId, request, ct);
        return skill.ToVm();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet]
    public async Task<LearnerReportVm> Get(CancellationToken ct)
    {
        var report = await service.GetLearnerReportAsync(CurrentUserId, ct);
        return report.ToVm();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/learner/reminders")]
public sealed class LearnerRemindersController(IReminderService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<MentorReminderVm>> Get(CancellationToken ct)
    {
        var reminders = await service.GetUnreadForLearnerAsync(CurrentUserId, ct);
        return reminders.Select(x => x.ToVm()).ToArray();
    }

    [HttpPost("{id:guid}/viewed")]
    public Task Viewed(Guid id, CancellationToken ct)
    {
        return service.MarkReadAsync(CurrentUserId, id, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Mentor")]
[Route("api/mentor")]
public sealed class MentorPortalController(
    IMentorService service,
    IReminderService reminders) : ControllerBase
{
    [HttpGet("invitations")]
    public async Task<IReadOnlyCollection<MentorLearnerVm>> Invitations(CancellationToken ct)
    {
        var invitations = await service.GetPendingInvitationsForMentorAsync(CurrentUserId, ct);
        return invitations.Select(x => x.ToVm()).ToArray();
    }

    [HttpPost("invitations/{id:guid}/accept")]
    public async Task<AuthResponseVm> Accept(Guid id, CancellationToken ct)
    {
        var response = await service.AcceptInvitationAsync(id, CurrentUserId, ct);
        return response.ToVm();
    }

    [HttpPost("invitations/{id:guid}/decline")]
    public Task Decline(Guid id, CancellationToken ct)
    {
        return service.DeclineInvitationAsync(id, CurrentUserId, ct);
    }

    [HttpGet("mentees")]
    public async Task<IReadOnlyCollection<LearnerSummaryVm>> Mentees(CancellationToken ct)
    {
        var mentees = await service.GetActiveMenteesAsync(CurrentUserId, ct);
        return mentees.Select(x => x.ToVm()).ToArray();
    }

    [HttpGet("learners/{learnerId:guid}")]
    public async Task<LearnerDashboardVm> Dashboard(Guid learnerId, CancellationToken ct)
    {
        var dashboard = await service.GetLearnerDashboardReadOnlyAsync(CurrentUserId, learnerId, ct);
        return dashboard.ToVm();
    }

    [HttpPost("learners/{learnerId:guid}/fading-reminder")]
    public Task SendReminder(Guid learnerId, CancellationToken ct)
    {
        return reminders.SendFadingSkillReminderAsync(CurrentUserId, learnerId, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
