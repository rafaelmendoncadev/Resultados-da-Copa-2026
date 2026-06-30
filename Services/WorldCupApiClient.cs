using System.Net.Http.Json;
using System.Text.Json;
using Resultados_da_Copa_2026.Models;

namespace Resultados_da_Copa_2026.Services;

public class WorldCupApiClient
{
    private const string BaseUrl = "https://worldcup26.ir";

    private readonly HttpClient _httpClient;

    public WorldCupApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    }

    public async Task<List<Game>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync("/get/games", AppJsonContext.Default.GamesResponse, cancellationToken);
        return response?.Games ?? [];
    }

    public async Task<Game?> GetGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        using var httpResponse = await _httpClient.GetAsync($"/get/game/{gameId}", cancellationToken);
        if (!httpResponse.IsSuccessStatusCode)
            return null;

        var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("game", out var gameElement))
            return gameElement.Deserialize(AppJsonContext.Default.Game);

        return null;
    }

    public async Task<List<GroupStanding>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync("/get/groups", AppJsonContext.Default.GroupsResponse, cancellationToken);
        return response?.Groups ?? [];
    }

    public async Task<List<Team>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync("/get/teams", AppJsonContext.Default.TeamsResponse, cancellationToken);
        return response?.Teams ?? [];
    }

    public async Task<List<Stadium>> GetStadiumsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync("/get/stadiums", AppJsonContext.Default.StadiumsResponse, cancellationToken);
        return response?.Stadiums ?? [];
    }
}
