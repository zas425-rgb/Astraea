using System.Security.Claims;
using Astraea.Application;
using Astraea.Application.Study;
using Astraea.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/studylogs")]
public sealed class StudyLogsController(IStudyLogService service) : ControllerBase
{
    [HttpPost]
    public async Task<CelestialNodeVm> Create(StudyLogCreateDto request, CancellationToken ct)
    {
        var updatedSkill = await service.RecordAsync(CurrentUserId, request, ct);
        return updatedSkill.ToVm();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
