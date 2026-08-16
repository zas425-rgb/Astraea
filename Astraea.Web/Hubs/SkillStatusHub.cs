using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.SignalR;
namespace Astraea.Web.Hubs;
[Authorize] public sealed class SkillStatusHub:Hub { public Task SubscribeToLearner(Guid learnerId)=>Groups.AddToGroupAsync(Context.ConnectionId,$"learner:{learnerId}"); }
