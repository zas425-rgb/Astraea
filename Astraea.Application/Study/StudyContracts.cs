using Astraea.Application;

namespace Astraea.Application.Study;
public record StudyLogCreateDto(Guid SkillId,DateTime StudiedAtUtc,int SelfRating,string Notes);
public interface IStudyLogService { Task<CelestialNodeDto> RecordAsync(Guid learnerId,StudyLogCreateDto request,CancellationToken ct); }
