using System.Security.Claims;
using Astraea.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astraea.Web;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public Task<AuthResponseDto> Register(RegisterUserRequest request, CancellationToken ct)
    {
        return service.RegisterAsync(request, ct);
    }

    [HttpPost("login")]
    public Task<AuthResponseDto> Login(LoginRequest request, CancellationToken ct)
    {
        return service.LoginAsync(request, ct);
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
    public Task<IReadOnlyCollection<MentorLearnerDto>> Get(CancellationToken ct)
    {
        return service.GetLearnerMentorsAsync(CurrentUserId, ct);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<MentorLearnerDto>> Invite(
        MentorInviteRequest request,
        CancellationToken ct)
    {
        try
        {
            var invite = await service.InviteMentorAsync(CurrentUserId, request.Email, ct);
            return Ok(invite);
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
    public Task<IReadOnlyCollection<CelestialNodeDto>> Get(CancellationToken ct)
    {
        return service.GetSkillsAsync(CurrentUserId, ct);
    }

    [HttpPost]
    public Task<CelestialNodeDto> Create(CreateSkillRequest request, CancellationToken ct)
    {
        return service.CreateSkillAsync(CurrentUserId, request, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet]
    public Task<LearnerReportDto> Get(CancellationToken ct)
    {
        return service.GetLearnerReportAsync(CurrentUserId, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/learner/reminders")]
public sealed class LearnerRemindersController(IReminderService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<MentorReminderDto>> Get(CancellationToken ct)
    {
        return service.GetUnreadForLearnerAsync(CurrentUserId, ct);
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
    public Task<IReadOnlyCollection<MentorLearnerDto>> Invitations(CancellationToken ct)
    {
        return service.GetPendingInvitationsForMentorAsync(CurrentUserId, ct);
    }

    [HttpPost("invitations/{id:guid}/accept")]
    public Task<AuthResponseDto> Accept(Guid id, CancellationToken ct)
    {
        return service.AcceptInvitationAsync(id, CurrentUserId, ct);
    }

    [HttpPost("invitations/{id:guid}/decline")]
    public Task Decline(Guid id, CancellationToken ct)
    {
        return service.DeclineInvitationAsync(id, CurrentUserId, ct);
    }

    [HttpGet("mentees")]
    public Task<IReadOnlyCollection<LearnerSummaryDto>> Mentees(CancellationToken ct)
    {
        return service.GetActiveMenteesAsync(CurrentUserId, ct);
    }

    [HttpGet("learners/{learnerId:guid}")]
    public Task<LearnerDashboardDto> Dashboard(Guid learnerId, CancellationToken ct)
    {
        return service.GetLearnerDashboardReadOnlyAsync(CurrentUserId, learnerId, ct);
    }

    [HttpPost("learners/{learnerId:guid}/fading-reminder")]
    public Task SendReminder(Guid learnerId, CancellationToken ct)
    {
        return reminders.SendFadingSkillReminderAsync(CurrentUserId, learnerId, ct);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
