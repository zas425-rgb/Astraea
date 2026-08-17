using System.Security.Claims; using Astraea.Application; using Astraea.Application.Study; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Astraea.Web;
[ApiController][Authorize(Roles="Learner")][Route("api/studylogs")] public sealed class StudyLogsController(IStudyLogService service):ControllerBase { [HttpPost] public Task<CelestialNodeDto> Create(StudyLogCreateDto request,CancellationToken ct)=>service.RecordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),request,ct); }
