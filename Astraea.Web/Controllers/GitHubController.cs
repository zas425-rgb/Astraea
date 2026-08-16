using System.Security.Claims;
using Astraea.Application;
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
    public Task<GitHubConnectionDto> Connection(CancellationToken ct) => github.GetConnectionAsync(Me, ct);

    [HttpPost("connect")]
    public Task<GitHubConnectionDto> Connect(GitHubConnectRequest request, CancellationToken ct) => github.ConnectAsync(Me, request, ct);

    [HttpPost("oauth/start")]
    public async Task<ActionResult<GitHubOAuthStartDto>> StartOAuth(CancellationToken ct)
    {
        try
        {
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/github/callback";
            return Ok(await github.BeginOAuthAsync(Me, callbackUrl, ct));
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
    public Task<GitHubSyncResultDto> Sync(CancellationToken ct) => github.SyncAsync(Me, ct);

    [HttpDelete("connection")]
    public Task Disconnect(CancellationToken ct) => github.DisconnectAsync(Me, ct);
}
