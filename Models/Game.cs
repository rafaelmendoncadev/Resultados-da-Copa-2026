using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Models;

public class Game
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("home_team_id")]
    public string HomeTeamId { get; set; } = string.Empty;

    [JsonPropertyName("away_team_id")]
    public string AwayTeamId { get; set; } = string.Empty;

    [JsonPropertyName("home_score")]
    public string HomeScore { get; set; } = "0";

    [JsonPropertyName("away_score")]
    public string AwayScore { get; set; } = "0";

    [JsonPropertyName("home_scorers")]
    public string? HomeScorers { get; set; }

    [JsonPropertyName("away_scorers")]
    public string? AwayScorers { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("matchday")]
    public string Matchday { get; set; } = string.Empty;

    [JsonPropertyName("local_date")]
    public string LocalDate { get; set; } = string.Empty;

    [JsonPropertyName("stadium_id")]
    public string StadiumId { get; set; } = string.Empty;

    [JsonPropertyName("finished")]
    public string Finished { get; set; } = "FALSE";

    [JsonPropertyName("time_elapsed")]
    public string TimeElapsed { get; set; } = "notstarted";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "group";

    [JsonPropertyName("home_team_name_en")]
    public string? HomeTeamNameEn { get; set; }

    [JsonPropertyName("away_team_name_en")]
    public string? AwayTeamNameEn { get; set; }

    [JsonPropertyName("home_team_label")]
    public string? HomeTeamLabel { get; set; }

    [JsonPropertyName("away_team_label")]
    public string? AwayTeamLabel { get; set; }

    [JsonIgnore]
    public MatchStage Stage => MatchStageExtensions.FromApiType(Type);

    [JsonIgnore]
    public bool IsFinished => Finished.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsLive => !IsFinished &&
        !TimeElapsed.Equals("notstarted", StringComparison.OrdinalIgnoreCase) &&
        !TimeElapsed.Equals("finished", StringComparison.OrdinalIgnoreCase);

    public string GetHomeDisplayName() =>
        !string.IsNullOrWhiteSpace(HomeTeamNameEn) ? HomeTeamNameEn :
        !string.IsNullOrWhiteSpace(HomeTeamLabel) ? HomeTeamLabel : "A definir";

    public string GetAwayDisplayName() =>
        !string.IsNullOrWhiteSpace(AwayTeamNameEn) ? AwayTeamNameEn :
        !string.IsNullOrWhiteSpace(AwayTeamLabel) ? AwayTeamLabel : "A definir";

    public bool HasTeamsDefined() =>
        HomeTeamId != "0" && AwayTeamId != "0";
}

public class GamesResponse
{
    [JsonPropertyName("games")]
    public List<Game> Games { get; set; } = [];
}
