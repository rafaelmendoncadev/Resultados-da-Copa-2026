using System.Net.Http.Json;
using System.Text.Json;
using Resultados_da_Copa_2026.Models;

namespace Resultados_da_Copa_2026.Services;

public class OpenFootballClient
{
    private const string Url =
        "https://raw.githubusercontent.com/openfootball/worldcup.json/master/2026/worldcup.json";

    private readonly HttpClient _httpClient;

    public OpenFootballClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<List<Game>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var root = await _httpClient.GetFromJsonAsync(Url, AppJsonContext.Default.OpenFootballRoot, cancellationToken);
        if (root?.Matches == null)
            return [];

        return root.Matches.Select((match, index) => MapToGame(match, index + 1)).ToList();
    }

    private static Game MapToGame(OpenFootballMatch match, int id)
    {
        var isKnockout = match.Group == null || !match.Group.StartsWith("Group", StringComparison.OrdinalIgnoreCase);
        var groupLetter = match.Group?.Replace("Group ", "", StringComparison.OrdinalIgnoreCase).Trim() ?? "";

        var stage = isKnockout ? InferKnockoutType(match.Round) : "group";
        var ft = match.Score?.FullTime;

        return new Game
        {
            Id = id.ToString(),
            HomeTeamNameEn = match.Team1,
            AwayTeamNameEn = match.Team2,
            HomeScore = ft != null && ft.Length > 0 ? ft[0].ToString() : "0",
            AwayScore = ft != null && ft.Length > 1 ? ft[1].ToString() : "0",
            HomeScorers = FormatGoals(match.Goals1),
            AwayScorers = FormatGoals(match.Goals2),
            Group = isKnockout ? stage.ToUpperInvariant() : groupLetter,
            Matchday = match.Round,
            LocalDate = $"{match.Date} {match.Time}",
            Finished = ft != null ? "TRUE" : "FALSE",
            TimeElapsed = ft != null ? "finished" : "notstarted",
            Type = stage,
            HomeTeamId = "1",
            AwayTeamId = "2"
        };
    }

    private static string InferKnockoutType(string round)
    {
        var r = round.ToLowerInvariant();
        if (r.Contains("round of 32") || r.Contains("round of thirty"))
            return "r32";
        if (r.Contains("round of 16"))
            return "r16";
        if (r.Contains("quarter"))
            return "qf";
        if (r.Contains("semi"))
            return "sf";
        if (r.Contains("3rd") || r.Contains("third"))
            return "3rd";
        if (r.Contains("final"))
            return "final";
        return "group";
    }

    private static string? FormatGoals(List<OpenFootballGoal>? goals)
    {
        if (goals == null || goals.Count == 0)
            return "null";

        var scorers = goals.Select(g => $"{g.Name} {g.Minute}'");
        return "{\"" + string.Join("\",\"", scorers) + "\"}";
    }
}
