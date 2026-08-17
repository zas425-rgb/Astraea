using Astraea.Application.Retention;
using Astraea.Domain;
using Xunit;

namespace Astraea.Tests;

public sealed class RetentionServiceTests
{
    private readonly RetentionService service = new();

    [Fact]
    public void Retention_decreases()
    {
        var result = service.CalculateRetention(
            DateTime.UtcNow.AddDays(-7),
            7,
            DateTime.UtcNow);

        Assert.InRange(result, 36, 38);
    }

    [Fact]
    public void At_risk_is_below_forty()
    {
        Assert.Equal(RiskStatus.AtRisk, service.GetRiskStatus(39.9));
    }

    [Fact]
    public void Excellent_review_expands_interval()
    {
        var result = service.ApplyReview(2.5, 6, 5);

        Assert.True(result.IntervalDays > 6);
    }
}
