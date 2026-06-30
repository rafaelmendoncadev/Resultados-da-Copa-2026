using System.Globalization;

namespace Resultados_da_Copa_2026.Services;

public static class GameDisplayHelper
{
    /// <summary>
    /// Mapa de cidades-sede da Copa 2026 para o deslocamento em horas
    /// que deve ser ADICIONADO ao horário local do estádio para obter o
    /// horário de Brasília (BRT, UTC-3).
    /// </summary>
    private static readonly Dictionary<string, int> CityToBrasiliaOffset = new(StringComparer.OrdinalIgnoreCase)
    {
        // ═══ EDT (UTC-4) → BRT = local + 1h ═══
        // EUA — Costa Leste
        { "new york", 1 }, { "east rutherford", 1 },
        { "philadelphia", 1 }, { "foxborough", 1 }, { "boston", 1 },
        { "landover", 1 }, { "washington", 1 }, { "baltimore", 1 },
        { "atlanta", 1 }, { "miami", 1 }, { "orlando", 1 },
        // Canadá — Leste
        { "toronto", 1 }, { "montreal", 1 },

        // ═══ CDT (UTC-5) → BRT = local + 2h ═══
        // EUA — Central
        { "dallas", 2 }, { "arlington", 2 },
        { "houston", 2 }, { "kansas city", 2 }, { "chicago", 2 },
        { "indianapolis", 2 }, { "nashville", 2 },
        // México (NÃO observa horário de verão desde 2022 — UTC-6 fixo todo o ano)
        { "mexico city", 3 }, { "guadalajara", 3 }, { "monterrey", 3 },

        // ═══ MDT (UTC-6) → BRT = local + 3h ═══
        { "denver", 3 }, { "salt lake city", 3 },

        // ═══ PDT (UTC-7) → BRT = local + 4h ═══
        // EUA — Costa Oeste
        { "los angeles", 4 }, { "inglewood", 4 },
        { "santa clara", 4 }, { "san francisco", 4 }, { "san jose", 4 },
        { "seattle", 4 },
        // Canadá — Oeste
        { "vancouver", 4 }
    };

    public static string GetStatusText(Models.Game game)
    {
        if (game.IsLive)
            return "AO VIVO";

        if (game.IsFinished)
            return "Encerrado";

        return game.TimeElapsed switch
        {
            "notstarted" => "Agendado",
            _ => game.TimeElapsed
        };
    }

    public static string FormatScore(Models.Game game)
    {
        if (!game.IsFinished && !game.IsLive && game.TimeElapsed == "notstarted")
            return "vs";

        return $"{game.HomeScore} - {game.AwayScore}";
    }

    /// <summary>
    /// Formata a data exibindo no HORÁRIO DE BRASÍLIA (BRT, UTC-3).
    /// </summary>
    /// <param name="localDate">Data/hora local do estádio vinda da API.</param>
    /// <param name="stadiumId">ID do estádio para lookup no mapa de cidades.</param>
    /// <param name="stadiumCities">Dicionário opcional (stadiumId → nome da cidade).</param>
    public static string FormatDate(string localDate, string? stadiumId = null,
        Dictionary<string, string>? stadiumCities = null)
    {
        if (!TryParseApiDate(localDate, out var dt))
            return localDate;

        // Descobre o fuso pelo estádio
        int offsetHours = 0;
        if (stadiumId != null && stadiumCities != null &&
            stadiumCities.TryGetValue(stadiumId, out var city) &&
            CityToBrasiliaOffset.TryGetValue(city, out var found))
        {
            offsetHours = found;
        }

        var brasiliaTime = dt.AddHours(offsetHours);
        return brasiliaTime.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Versão sem conversão de fuso — apenas formatação simples.
    /// Útil onde o fuso não é relevante.
    /// </summary>
    public static string FormatDateRaw(string localDate)
    {
        if (TryParseApiDate(localDate, out var dt))
            return dt.ToString("dd/MM/yyyy HH:mm");
        return localDate;
    }

    /// <summary>
    /// Extrai apenas a data (sem hora) no formato dd/MM.
    /// </summary>
    public static string FormatShortDate(string localDate, string? stadiumId = null,
        Dictionary<string, string>? stadiumCities = null)
    {
        if (!TryParseApiDate(localDate, out var dt))
            return localDate;

        int offsetHours = 0;
        if (stadiumId != null && stadiumCities != null &&
            stadiumCities.TryGetValue(stadiumId, out var city) &&
            CityToBrasiliaOffset.TryGetValue(city, out var found))
        {
            offsetHours = found;
        }

        var brasiliaTime = dt.AddHours(offsetHours);
        return brasiliaTime.ToString("dd/MM");
    }

    /// <summary>
    /// Tenta interpretar a data da API (formato MM/dd/yyyy HH:mm ou dd/MM/yyyy HH:mm, etc.).
    /// </summary>
    private static bool TryParseApiDate(string localDate, out DateTime result)
    {
        var formats = new[]
        {
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy H:mm",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy H:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd H:mm",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd"
        };
        return DateTime.TryParseExact(localDate, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);
    }

    public static List<string> ParseScorers(string? scorersJson)
    {
        if (string.IsNullOrWhiteSpace(scorersJson) || scorersJson == "null")
            return [];

        var cleaned = scorersJson.Trim('{', '}', '"');
        if (string.IsNullOrWhiteSpace(cleaned))
            return [];

        return cleaned
            .Split("\",\"", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim('"', ' '))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    public static string GetHomeName(Models.Game game) =>
        game.HomeTeamId == "0"
            ? TeamNameMapper.TranslateKnockoutLabel(game.HomeTeamLabel)
            : TeamNameMapper.ToPortuguese(game.GetHomeDisplayName());

    public static string GetAwayName(Models.Game game) =>
        game.AwayTeamId == "0"
            ? TeamNameMapper.TranslateKnockoutLabel(game.AwayTeamLabel)
            : TeamNameMapper.ToPortuguese(game.GetAwayDisplayName());
}
