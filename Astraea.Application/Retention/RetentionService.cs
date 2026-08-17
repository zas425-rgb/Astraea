using Astraea.Domain;

namespace Astraea.Application.Retention;

public sealed class RetentionService : IRetentionService
{
    public double CalculateRetention(DateTime reviewed, int interval, DateTime now)
    {
        var elapsedDays = Math.Max(0, (now - reviewed).TotalDays);
        var safeInterval = Math.Max(interval, 1);
        var retentionPercent = 100 * Math.Exp(-elapsedDays / safeInterval);

        return Math.Round(Math.Clamp(retentionPercent, 0, 100), 1);
    }

    public RiskStatus GetRiskStatus(double value)
    {
        return value >= 70
            ? RiskStatus.Fresh
            : value >= 40
                ? RiskStatus.Fading
                : RiskStatus.AtRisk;
    }

    public (double EaseFactor, int IntervalDays) ApplyReview(
        double ease,
        int previous,
        int quality)
    {
        if (quality is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(quality));
        }

        var nextEase = Math.Max(
            1.3,
            ease + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));

        var nextInterval = quality < 3
            ? 1
            : previous <= 1
                ? 6
                : (int)Math.Ceiling(previous * nextEase);

        return (Math.Round(nextEase, 2), nextInterval);
    }
}
