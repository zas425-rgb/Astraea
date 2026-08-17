using Astraea.Domain;

namespace Astraea.Application.Retention;

public record RetentionCurvePointDto(
    DateTime DateUtc,
    double RetentionPercent,
    bool IsReviewMarker);

public record SkillStatusDeltaDto(
    Guid SkillId,
    double RetentionPercent,
    RiskStatus RiskStatus,
    DateTime NextReviewDueDateUtc);

public interface IRetentionService
{
    double CalculateRetention(
        DateTime lastReviewedUtc,
        int intervalDays,
        DateTime nowUtc);

    RiskStatus GetRiskStatus(double retentionPercent);

    (double EaseFactor, int IntervalDays) ApplyReview(
        double easeFactor,
        int priorIntervalDays,
        int quality);
}
