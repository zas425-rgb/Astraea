using Astraea.Application.Retention;
using Astraea.Infrastructure.Background;
using Astraea.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
namespace Astraea.Web.Services;

public sealed class SignalRSkillStatusPublisher(IHubContext<SkillStatusHub> hub) : ISkillStatusPublisher { public Task PublishAsync(Guid learnerId, SkillStatusDeltaDto delta, CancellationToken ct) => hub.Clients.Group($"learner:{learnerId}").SendAsync("skillStatusChanged", delta, ct); }
