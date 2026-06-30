using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Models;

public class OpenFootballRoot
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("matches")]
    public List<OpenFootballMatch> Matches { get; set; } = [];
}

public class OpenFootballMatch
{
    [JsonPropertyName("round")]
    public string Round { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("team1")]
    public string Team1 { get; set; } = string.Empty;

    [JsonPropertyName("team2")]
    public string Team2 { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public OpenFootballScore? Score { get; set; }

    [JsonPropertyName("goals1")]
    public List<OpenFootballGoal>? Goals1 { get; set; }

    [JsonPropertyName("goals2")]
    public List<OpenFootballGoal>? Goals2 { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("ground")]
    public string? Ground { get; set; }
}

public class OpenFootballScore
{
    [JsonPropertyName("ft")]
    public int[]? FullTime { get; set; }

    [JsonPropertyName("ht")]
    public int[]? HalfTime { get; set; }

    /// <summary>Placar da prorrogação (extra time). Ex: [1, 1] significa 1-1 na prorrogação.</summary>
    [JsonPropertyName("et")]
    public int[]? ExtraTime { get; set; }

    /// <summary>Placar dos pênaltis. Ex: [3, 4] significa 3-4 nos pênaltis.</summary>
    [JsonPropertyName("p")]
    public int[]? Penalties { get; set; }
}

public class OpenFootballGoal
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("minute")]
    public string Minute { get; set; } = string.Empty;
}
