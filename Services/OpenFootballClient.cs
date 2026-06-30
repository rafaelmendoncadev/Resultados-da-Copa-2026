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
        var sc = match.Score;
        var ft = sc?.FullTime;
        var pen = sc?.Penalties;

        // Placar final (ft inclui gols da prorrogação, se houver)
        var hasScore = ft != null && ft.Length >= 2;

        var homeScore = hasScore ? ft![0] : 0;
        var awayScore = hasScore ? ft![1] : 0;

        return new Game
        {
            Id = id.ToString(),
            HomeTeamNameEn = match.Team1,
            AwayTeamNameEn = match.Team2,
            HomeScore = homeScore.ToString(),
            AwayScore = awayScore.ToString(),
            HomeScorers = FormatGoals(match.Goals1),
            AwayScorers = FormatGoals(match.Goals2),
            Group = isKnockout ? stage.ToUpperInvariant() : groupLetter,
            Matchday = match.Round,
            LocalDate = $"{match.Date} {match.Time}",
            Finished = hasScore ? "TRUE" : "FALSE",
            TimeElapsed = hasScore ? "finished" : "notstarted",
            Type = stage,
            HomeTeamId = "1",
            AwayTeamId = "2",
            HomePenaltyScore = pen != null && pen.Length >= 2 ? pen[0] : null,
            AwayPenaltyScore = pen != null && pen.Length >= 2 ? pen[1] : null,
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
