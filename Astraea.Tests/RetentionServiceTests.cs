using Astraea.Application.Retention; using Astraea.Domain; using Xunit;
namespace Astraea.Tests;
public sealed class RetentionServiceTests { readonly RetentionService Service=new(); [Fact] public void Retention_decreases(){Assert.InRange(Service.CalculateRetention(DateTime.UtcNow.AddDays(-7),7,DateTime.UtcNow),36,38);} [Fact] public void At_risk_is_below_forty(){Assert.Equal(RiskStatus.AtRisk,Service.GetRiskStatus(39.9));} [Fact] public void Excellent_review_expands_interval(){Assert.True(Service.ApplyReview(2.5,6,5).IntervalDays>6);} }
