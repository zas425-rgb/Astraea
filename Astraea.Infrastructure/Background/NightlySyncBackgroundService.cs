using Astraea.Application;
using Astraea.Application.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astraea.Infrastructure.Background;

public interface ISkillStatusPublisher
{
    Task PublishAsync(Guid learnerId, SkillStatusDeltaDto delta, CancellationToken ct);
}

public sealed class NightlySyncBackgroundService(
    IServiceScopeFactory scopes,
    ISkillStatusPublisher publisher,
    IRetentionService retention) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncGitHubAndPublishRetentionAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task SyncGitHubAndPublishRetentionAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AstraeaDbContext>();
        var github = scope.ServiceProvider.GetRequiredService<IGitHubSyncService>();

        var learnerIds = await db.GitHubConnections.AsNoTracking().Select(x => x.LearnerId).ToListAsync(ct);
        foreach (var learnerId in learnerIds)
        {
            try
            {
                await github.SyncAsync(learnerId, ct);
            }
            catch
            {
            }
        }

        foreach (var skill in await db.Skills.AsNoTracking().Where(x => !x.IsArchived).ToListAsync(ct))
        {
            var value = retention.CalculateRetention(skill.LastReviewedUtc, skill.CurrentIntervalDays, DateTime.UtcNow);
            await publisher.PublishAsync(skill.LearnerId, new(skill.Id, value, retention.GetRiskStatus(value), skill.NextReviewDueDateUtc), ct);
        }
    }
}
