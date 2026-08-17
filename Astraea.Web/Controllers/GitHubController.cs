using System.Security.Claims;
using Astraea.Application;
using Astraea.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astraea.Web;

[ApiController]
[Authorize(Roles = "Learner")]
[Route("api/github")]
public sealed class GitHubController(IGitHubSyncService github) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("connection")]
    public async Task<GitHubConnectionVm> Connection(CancellationToken ct)
    {
        var connection = await github.GetConnectionAsync(Me, ct);
        return connection.ToVm();
    }

    [HttpPost("connect")]
    public async Task<GitHubConnectionVm> Connect(GitHubConnectRequest request, CancellationToken ct)
    {
        var connection = await github.ConnectAsync(Me, request, ct);
        return connection.ToVm();
    }

    [HttpPost("oauth/start")]
    public async Task<ActionResult<GitHubOAuthStartVm>> StartOAuth(CancellationToken ct)
    {
        try
        {
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/github/callback";
            var start = await github.BeginOAuthAsync(Me, callbackUrl, ct);
            return Ok(start.ToVm());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("GitHub:Client", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("GitHub OAuth is not configured yet. Add GitHub:ClientId and GitHub:ClientSecret, then restart Astraea.");
        }
    }

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        await github.CompleteOAuthAsync(code, state, ct);
        return Redirect("/astraea-platform.html?github=connected");
    }

    [HttpPost("sync")]
    public async Task<GitHubSyncResultVm> Sync(CancellationToken ct)
    {
        var result = await github.SyncAsync(Me, ct);
        return result.ToVm();
    }

    [HttpDelete("connection")]
    public Task Disconnect(CancellationToken ct)
    {
        return github.DisconnectAsync(Me, ct);
    }
}
