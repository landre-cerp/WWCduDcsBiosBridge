using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WWCduDcsBiosBridge.Services;

public sealed class GitHubUpdateService
{
    private readonly string owner;
    private readonly string repo;
    private readonly HttpClient http;

    public GitHubUpdateService(string owner, string repo, HttpClient? httpClient = null)
    {
        this.owner = owner;
        this.repo = repo;
        http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{repo}/UpdateCheck");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<UpdateCheckResult?> CheckForUpdatesAsync(string currentVersion, CancellationToken ct = default)
    {
        var latest = await GetLatestReleaseAsync(ct);
        if (latest?.TagName is null) return null;

        var hasUpdate = IsNewerVersion(latest.TagName, currentVersion);
        return new UpdateCheckResult(hasUpdate, latest.TagName, latest.HtmlUrl);
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken ct)
    {
        // 1) Try latest stable endpoint
        var latestUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        try
        {
            using var latestStream = await http.GetStreamAsync(latestUrl, ct);
            var latest = await JsonSerializer.DeserializeAsync<GitHubRelease>(latestStream, cancellationToken: ct);
            if (latest is { Draft: false, Prerelease: false, TagName: { Length: > 0 } })
                return latest;
        }
        catch
        {
            // fall through
        }

        // 2) Fallback to list and pick highest stable semver (or prerelease if no stable)
        var listUrl = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=20";
        try
        {
            using var listStream = await http.GetStreamAsync(listUrl, ct);
            var all = await JsonSerializer.DeserializeAsync<List<GitHubRelease>>(listStream, cancellationToken: ct);
            if (all is null) return null;

            var stable = all.Where(r => r is { Draft: false, Prerelease: false })
                            .OrderByDescending(r => ParseVersionOrDefault(r.TagName))
                            .FirstOrDefault();
            if (stable is not null) return stable;

            return all.Where(r => r is { Draft: false, Prerelease: true })
                      .OrderByDescending(r => ParseVersionOrDefault(r.TagName))
                      .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static Version ParseVersionOrDefault(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return new Version(0, 0, 0, 0);
        var t = tag.Trim();
        if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase)) t = t[1..];
        t = t.Split('+', '-')[0];
        if (!Version.TryParse(t, out var v))
        {
            if (int.TryParse(t, out var majorOnly)) return new Version(majorOnly, 0);
            return new Version(0, 0, 0, 0);
        }
        return v;
    }

    private static bool IsNewerVersion(string latestTag, string current)
    {
        var latestV = ParseVersionOrDefault(latestTag);
        var currentV = ParseVersionOrDefault(current);
        return latestV > currentV;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
    }
}

public sealed record UpdateCheckResult(bool HasUpdate, string? LatestTag, string? HtmlUrl);