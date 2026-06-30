using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Models;

public class Team
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("flag")]
    public string? Flag { get; set; }

    [JsonPropertyName("fifa_code")]
    public string? FifaCode { get; set; }

    [JsonPropertyName("groups")]
    public string? Group { get; set; }
}

public class TeamsResponse
{
    [JsonPropertyName("teams")]
    public List<Team> Teams { get; set; } = [];
}
