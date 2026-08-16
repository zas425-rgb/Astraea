using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Astraea.Application;
using Astraea.Domain;
using Astraea.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Web;

[ApiController]
[Route("api/auth/github")]
public sealed class GitHubAuthController(
    AstraeaDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider,
    IGitHubSyncService githubSync,
    ITokenFactory tokens) : ControllerBase
{
    private readonly IDataProtector stateProtector = dataProtectionProvider.CreateProtector("Astraea.GitHub.AuthState.v1");
    private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("Astraea.GitHub.AccessTokens.v1");

    [HttpPost("start")]
    public ActionResult<GitHubOAuthStartDto> Start()
    {
        var clientId = configuration["GitHub:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(configuration["GitHub:ClientSecret"]))
        {
            return BadRequest("GitHub OAuth is not configured yet. Add GitHub:ClientId and GitHub:ClientSecret, then restart Astraea.");
        }

        var state = stateProtector.Protect(JsonSerializer.Serialize(new GitHubAuthState(DateTime.UtcNow.AddMinutes(10), CreateState())));
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/github/callback";
        var url = "https://github.com/login/oauth/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
            $"&scope={Uri.EscapeDataString("read:user user:email repo")}" +
            $"&state={Uri.EscapeDataString(state)}";
        return Ok(new GitHubOAuthStartDto(url));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        GitHubAuthState? authState;
        try
        {
            authState = JsonSerializer.Deserialize<GitHubAuthState>(stateProtector.Unprotect(state));
        }
        catch (CryptographicException)
        {
            await githubSync.CompleteOAuthAsync(code, state, ct);
            return Redirect("/astraea-platform.html?github=connected");
        }

        if (authState is null || authState.ExpiresAtUtc < DateTime.UtcNow)
        {
            return BadRequest("GitHub sign-in expired. Please try again.");
        }

        var accessToken = await ExchangeCodeForTokenAsync(code, ct);
        var githubUser = await FetchAuthenticatedUserAsync(accessToken, ct);
        var email = await FetchPrimaryEmailAsync(accessToken, ct) ?? $"{githubUser.Login}@users.noreply.github.com";
        email = email.Trim().ToLowerInvariant();

        var connection = await db.GitHubConnections.Include(x => x.Learner).SingleOrDefaultAsync(x => x.GitHubUserId == githubUser.Id, ct);
        User user;
        if (connection is not null)
        {
            user = connection.Learner;
        }
        else
        {
            user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct) ?? new User
            {
                Email = email,
                FullName = string.IsNullOrWhiteSpace(githubUser.Name) ? githubUser.Login : githubUser.Name,
                Role = UserRole.Learner,
                PasswordHash = CreateExternalPasswordHash()
            };
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }
            if (db.Entry(user).State == EntityState.Detached)
            {
                db.Users.Add(user);
            }
        }

        var githubConnection = await db.GitHubConnections.SingleOrDefaultAsync(x => x.LearnerId == user.Id, ct);
        if (githubConnection is null)
        {
            githubConnection = new GitHubConnection { LearnerId = user.Id };
            db.GitHubConnections.Add(githubConnection);
        }

        githubConnection.GitHubUserId = githubUser.Id;
        githubConnection.GitHubUsername = githubUser.Login;
        githubConnection.IsOAuthVerified = true;
        githubConnection.AccessTokenProtected = tokenProtector.Protect(accessToken);
        githubConnection.ConnectedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var response = new AuthResponseDto(tokens.Create(user), user.Id, user.FullName, user.Role.ToString());
        var encoded = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return Redirect($"/astraea-platform.html?githubAuth={encoded}");
    }

    private async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken ct)
    {
        var response = await Client().PostAsJsonAsync("https://github.com/login/oauth/access_token", new
        {
            client_id = configuration["GitHub:ClientId"],
            client_secret = configuration["GitHub:ClientSecret"],
            code
        }, ct);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken: ct);
        return string.IsNullOrWhiteSpace(token?.AccessToken) ? throw new InvalidOperationException("GitHub did not return an access token.") : token.AccessToken;
    }

    private async Task<GitHubUserResponse> FetchAuthenticatedUserAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client().SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("GitHub user profile could not be read.");
    }

    private async Task<string?> FetchPrimaryEmailAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var emails = await response.Content.ReadFromJsonAsync<List<GitHubEmailResponse>>(cancellationToken: ct) ?? [];
        return emails.FirstOrDefault(x => x.Primary && x.Verified)?.Email ?? emails.FirstOrDefault(x => x.Verified)?.Email;
    }

    private HttpClient Client()
    {
        var client = httpClientFactory.CreateClient("github");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AstraeaSkillTracker/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static string CreateState()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string CreateExternalPasswordHash()
    {
        return "github:" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed record GitHubAuthState(DateTime ExpiresAtUtc, string Nonce);
    private sealed record GitHubTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private sealed record GitHubUserResponse(long Id, string Login, string? Name);
    private sealed record GitHubEmailResponse(string Email, bool Primary, bool Verified);
}
