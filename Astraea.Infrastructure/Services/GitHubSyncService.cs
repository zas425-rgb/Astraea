using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Astraea.Application;
using Astraea.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Astraea.Infrastructure;

public sealed class GitHubSyncService(
    AstraeaDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider) : IGitHubSyncService
{
    private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("Astraea.GitHub.AccessTokens.v1");

    public async Task<GitHubConnectionDto> GetConnectionAsync(Guid learnerId, CancellationToken ct)
    {
        var connection = await db.GitHubConnections.AsNoTracking().SingleOrDefaultAsync(x => x.LearnerId == learnerId, ct);
        return connection is null || string.IsNullOrWhiteSpace(connection.GitHubUsername) ? new(false, null, null, null, 0, 0, false) : ToDto(connection);
    }

    public async Task<GitHubConnectionDto> ConnectAsync(Guid learnerId, GitHubConnectRequest request, CancellationToken ct)
    {
        throw new InvalidOperationException("GitHub ownership must be verified with OAuth. Use the Connect GitHub button.");
    }

    public async Task<GitHubOAuthStartDto> BeginOAuthAsync(Guid learnerId, string callbackUrl, CancellationToken ct)
    {
        var clientId = RequiredSetting("GitHub:ClientId");
        var state = CreateState();
        var connection = await db.GitHubConnections.SingleOrDefaultAsync(x => x.LearnerId == learnerId, ct);
        if (connection is null)
        {
            connection = new GitHubConnection { LearnerId = learnerId };
            db.GitHubConnections.Add(connection);
        }

        connection.OAuthState = state;
        connection.OAuthStateExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync(ct);

        var url = "https://github.com/login/oauth/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
            $"&scope={Uri.EscapeDataString("repo read:user")}" +
            $"&state={Uri.EscapeDataString(state)}";
        return new GitHubOAuthStartDto(url);
    }

    public async Task CompleteOAuthAsync(string code, string state, CancellationToken ct)
    {
        var connection = await db.GitHubConnections.SingleOrDefaultAsync(x => x.OAuthState == state, ct)
            ?? throw new InvalidOperationException("GitHub connection state was not found.");
        if (connection.OAuthStateExpiresUtc is null || connection.OAuthStateExpiresUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("GitHub connection state expired. Please try again.");
        }

        var token = await ExchangeCodeForTokenAsync(code, ct);
        var githubUser = await FetchAuthenticatedUserAsync(token, ct);

        connection.GitHubUserId = githubUser.Id;
        connection.GitHubUsername = githubUser.Login;
        connection.IsOAuthVerified = true;
        connection.AccessTokenProtected = tokenProtector.Protect(token);
        connection.ConnectedAtUtc = DateTime.UtcNow;
        connection.OAuthState = null;
        connection.OAuthStateExpiresUtc = null;
        connection.LastSyncDateUtc = null;
        connection.LastReposScanned = 0;
        connection.LastSignalsImported = 0;
        await db.SaveChangesAsync(ct);
    }

    public async Task DisconnectAsync(Guid learnerId, CancellationToken ct)
    {
        var connection = await db.GitHubConnections.SingleOrDefaultAsync(x => x.LearnerId == learnerId, ct);
        if (connection is not null)
        {
            db.GitHubConnections.Remove(connection);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<GitHubSyncResultDto> SyncAsync(Guid learnerId, CancellationToken ct)
    {
        var connection = await db.GitHubConnections.SingleOrDefaultAsync(x => x.LearnerId == learnerId, ct)
            ?? throw new InvalidOperationException("Connect GitHub before syncing.");
        if (string.IsNullOrWhiteSpace(connection.GitHubUsername))
        {
            throw new InvalidOperationException("Connect GitHub before syncing.");
        }

        var skills = await db.Skills.Where(x => x.LearnerId == learnerId && !x.IsArchived).ToListAsync(ct);
        var repos = await FetchRepositoriesAsync(connection, ct);
        var imported = 0;

        foreach (var repo in repos)
        {
            var text = $"{repo.Name} {repo.Description}".ToLowerInvariant();
            foreach (var skill in skills.Where(skill => text.Contains(skill.Title.ToLowerInvariant())))
            {
                var externalReference = $"github:repo:{repo.Id}:{skill.Id}";
                if (await db.PracticeSignals.AnyAsync(x => x.SkillId == skill.Id && x.ExternalReference == externalReference, ct))
                {
                    continue;
                }

                db.PracticeSignals.Add(new PracticeSignal
                {
                    SkillId = skill.Id,
                    Source = PracticeSource.GitHub,
                    ExternalReference = externalReference,
                    OccurredAtUtc = repo.UpdatedAtUtc,
                    Hours = 1
                });
                if (repo.UpdatedAtUtc > skill.LastReviewedUtc)
                {
                    skill.LastReviewedUtc = repo.UpdatedAtUtc;
                    skill.NextReviewDueDateUtc = repo.UpdatedAtUtc.AddDays(skill.CurrentIntervalDays);
                }
                imported++;
            }
        }

        connection.LastSyncDateUtc = DateTime.UtcNow;
        connection.LastReposScanned = repos.Count;
        connection.LastSignalsImported = imported;
        await db.SaveChangesAsync(ct);
        return new GitHubSyncResultDto(connection.GitHubUsername, repos.Count, imported, connection.LastSyncDateUtc.Value);
    }

    private async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken ct)
    {
        var response = await Client().PostAsJsonAsync("https://github.com/login/oauth/access_token", new
        {
            client_id = RequiredSetting("GitHub:ClientId"),
            client_secret = RequiredSetting("GitHub:ClientSecret"),
            code
        }, ct);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken: ct);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("GitHub did not return an access token.");
        }
        return token.AccessToken;
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

    private async Task<IReadOnlyCollection<GitHubRepoInfo>> FetchRepositoriesAsync(GitHubConnection connection, CancellationToken ct)
    {
        var token = string.IsNullOrWhiteSpace(connection.AccessTokenProtected) ? null : tokenProtector.Unprotect(connection.AccessTokenProtected);
        var url = token is null
            ? $"https://api.github.com/users/{Uri.EscapeDataString(connection.GitHubUsername)}/repos?per_page=100&sort=updated"
            : "https://api.github.com/user/repos?per_page=100&sort=updated&affiliation=owner,collaborator&visibility=all";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await Client().SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepoResponse>>(cancellationToken: ct) ?? [];
        return repos.Select(x => new GitHubRepoInfo(x.Id, x.Name ?? "", x.Description ?? "", x.UpdatedAt.UtcDateTime)).ToArray();
    }

    private HttpClient Client()
    {
        var client = httpClientFactory.CreateClient("github");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AstraeaSkillTracker/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private string RequiredSetting(string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is not configured.");
        }
        return value;
    }

    private static GitHubConnectionDto ToDto(GitHubConnection connection)
    {
        return new(true, connection.GitHubUsername, connection.ConnectedAtUtc, connection.LastSyncDateUtc, connection.LastReposScanned, connection.LastSignalsImported, connection.IsOAuthVerified);
    }

    private static string CreateState()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed record GitHubRepoInfo(long Id, string Name, string Description, DateTime UpdatedAtUtc);
    private sealed record GitHubRepoResponse(long Id, string? Name, string? Description, [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt);
    private sealed record GitHubTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private sealed record GitHubUserResponse(long Id, string Login);
}
