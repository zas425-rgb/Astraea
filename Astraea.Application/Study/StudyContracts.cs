namespace Astraea.Application.Study;
public record StudyLogCreateDto(Guid SkillId,DateTime StudiedAtUtc,int SelfRating,string Notes);
public interface IStudyLogService { Task RecordAsync(Guid learnerId,StudyLogCreateDto request,CancellationToken ct); }
