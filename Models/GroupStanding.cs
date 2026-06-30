using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Models;

public class GroupStandingEntry
{
    [JsonPropertyName("team_id")]
    public string TeamId { get; set; } = string.Empty;

    [JsonPropertyName("mp")]
    public string MatchesPlayed { get; set; } = "0";

    [JsonPropertyName("w")]
    public string Wins { get; set; } = "0";

    [JsonPropertyName("d")]
    public string Draws { get; set; } = "0";

    [JsonPropertyName("l")]
    public string Losses { get; set; } = "0";

    [JsonPropertyName("pts")]
    public string Points { get; set; } = "0";

    [JsonPropertyName("gf")]
    public string GoalsFor { get; set; } = "0";

    [JsonPropertyName("ga")]
    public string GoalsAgainst { get; set; } = "0";

    [JsonPropertyName("gd")]
    public string GoalDifference { get; set; } = "0";

    [JsonIgnore]
    public string TeamName { get; set; } = string.Empty;
}

public class GroupStanding
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("teams")]
    public List<GroupStandingEntry> Teams { get; set; } = [];
}

public class GroupsResponse
{
    [JsonPropertyName("groups")]
    public List<GroupStanding> Groups { get; set; } = [];
}

public class Stadium
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("city_en")]
    public string? CityEn { get; set; }
}

public class StadiumsResponse
{
    [JsonPropertyName("stadiums")]
    public List<Stadium> Stadiums { get; set; } = [];
}
