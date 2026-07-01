using System.Text.Json;
using Android.Content;
using Resultados_da_Copa_2026.Models;

namespace Resultados_da_Copa_2026.Services;

public class DataResult<T>
{
    public T Data { get; init; } = default!;
    public bool FromCache { get; init; }
    public DateTime? CachedAt { get; init; }
    public bool IsOffline { get; init; }
}

public class MatchRepository
{
    private const string Tag = "MatchRepository";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly WorldCupApiClient _apiClient;
    private readonly OpenFootballClient _fallbackClient;
    private readonly string _cacheDirectory;

    public MatchRepository(Context context)
    {
        _apiClient = new WorldCupApiClient();
        _fallbackClient = new OpenFootballClient();
        _cacheDirectory = Path.Combine(context.CacheDir?.AbsolutePath ?? context.FilesDir?.AbsolutePath ?? ".", "worldcup_cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<DataResult<List<Game>>> GetGamesAsync(Context context, bool forceRefresh = false)
    {
        return await GetCachedOrFetchAsync(
            context,
            "games.json",
            () => _apiClient.GetGamesAsync(),
            () => _fallbackClient.GetGamesAsync(),
            forceRefresh);
    }

    public async Task<DataResult<List<GroupStanding>>> GetGroupsAsync(Context context, bool forceRefresh = false)
    {
        var result = await GetCachedOrFetchAsync(
            context,
            "groups.json",
            () => _apiClient.GetGroupsAsync(),
            async () => new List<GroupStanding>(),
            forceRefresh);

        if (result.Data.Count > 0)
            await EnrichStandingsWithTeamNames(context, result.Data);

        return result;
    }

    public async Task<DataResult<List<Team>>> GetTeamsAsync(Context context, bool forceRefresh = false)
    {
        return await GetCachedOrFetchAsync(
            context,
            "teams.json",
            () => _apiClient.GetTeamsAsync(),
            async () => new List<Team>(),
            forceRefresh);
    }

    public async Task<DataResult<List<Stadium>>> GetStadiumsAsync(Context context, bool forceRefresh = false)
    {
        return await GetCachedOrFetchAsync(
            context,
            "stadiums.json",
            () => _apiClient.GetStadiumsAsync(),
            async () => new List<Stadium>(),
            forceRefresh);
    }

    public async Task<Game?> GetGameByIdAsync(Context context, string gameId)
    {
        var gamesResult = await GetGamesAsync(context);
        return gamesResult.Data.FirstOrDefault(g => g.Id == gameId);
    }

    public async Task<string?> GetStadiumNameAsync(Context context, string stadiumId)
    {
        var stadiumsResult = await GetStadiumsAsync(context);
        return stadiumsResult.Data.FirstOrDefault(s => s.Id == stadiumId)?.NameEn;
    }

    private async Task EnrichStandingsWithTeamNames(Context context, List<GroupStanding> groups)
    {
        var teamsResult = await GetTeamsAsync(context);
        var teamMap = teamsResult.Data.ToDictionary(t => t.Id, t => t.NameEn);

        foreach (var group in groups)
        {
            foreach (var entry in group.Teams)
            {
                if (teamMap.TryGetValue(entry.TeamId, out var name))
                    entry.TeamName = TeamNameMapper.ToPortuguese(name);
                else
                    entry.TeamName = $"Time {entry.TeamId}";
            }
        }
    }

    private async Task<DataResult<List<T>>> GetCachedOrFetchAsync<T>(
        Context context,
        string cacheFileName,
        Func<Task<List<T>>> fetchPrimary,
        Func<Task<List<T>>> fetchFallback,
        bool forceRefresh)
    {
        var cachePath = Path.Combine(_cacheDirectory, cacheFileName);
        var isOnline = NetworkHelper.IsNetworkAvailable(context);

        if (!forceRefresh && File.Exists(cachePath))
        {
            var cacheAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
            if (cacheAge < CacheTtl)
            {
                var cached = await ReadCacheAsync<List<T>>(cachePath);
                if (cached != null)
                {
                    return new DataResult<List<T>>
                    {
                        Data = cached,
                        FromCache = true,
                        CachedAt = File.GetLastWriteTimeUtc(cachePath),
                        IsOffline = !isOnline
                    };
                }
            }
        }

        if (isOnline)
        {
            try
            {
                var data = await fetchPrimary();
                if (data.Count > 0)
                {
                    await WriteCacheAsync(cachePath, data);
                    return new DataResult<List<T>>
                    {
                        Data = data,
                        FromCache = false,
                        CachedAt = DateTime.UtcNow,
                        IsOffline = false
                    };
                }
            }
            catch (Exception ex)
            {
                // Falha na API primária (ex.: JSON inválido / rede). Registra para diagnóstico
                // e continua para o cache/fallback — antes isto era engolido silenciosamente,
                // o que mascarava mudanças de contrato da API como cache "desatualizado".
                Android.Util.Log.Warn(Tag, $"fetchPrimary ({cacheFileName}) falhou: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (File.Exists(cachePath))
        {
            var cached = await ReadCacheAsync<List<T>>(cachePath);
            if (cached != null)
            {
                return new DataResult<List<T>>
                {
                    Data = cached,
                    FromCache = true,
                    CachedAt = File.GetLastWriteTimeUtc(cachePath),
                    IsOffline = !isOnline
                };
            }
        }

        if (isOnline)
        {
            try
            {
                var fallbackData = await fetchFallback();
                if (fallbackData.Count > 0)
                {
                    await WriteCacheAsync(cachePath, fallbackData);
                    return new DataResult<List<T>>
                    {
                        Data = fallbackData,
                        FromCache = false,
                        CachedAt = DateTime.UtcNow,
                        IsOffline = false
                    };
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Tag, $"fetchFallback ({cacheFileName}) falhou: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return new DataResult<List<T>>
        {
            Data = [],
            FromCache = false,
            IsOffline = !isOnline
        };
    }

    private static async Task<T?> ReadCacheAsync<T>(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn(Tag, $"ReadCacheAsync falhou ({path}): {ex.GetType().Name}: {ex.Message}");
            return default;
        }
    }

    private static async Task WriteCacheAsync<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
