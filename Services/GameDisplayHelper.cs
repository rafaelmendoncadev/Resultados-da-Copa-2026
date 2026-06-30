using System.Globalization;

namespace Resultados_da_Copa_2026.Services;

public static class GameDisplayHelper
{
    /// <summary>
    /// Mapa de ID do estádio (conforme API worldcup26.ir) para o deslocamento em horas
    /// que deve ser ADICIONADO ao horário local do estádio para obter o
    /// horário de Brasília (BRT, UTC-3).
    /// 
    /// IDs 1-16 conforme dados oficiais da Copa 2026.
    /// 
    /// Fusos em junho/julho de 2026:
    ///   EDT (UTC-4) → BRT = local + 1h
    ///   CDT (UTC-5) → BRT = local + 2h
    ///   México (UTC-6 fixo) → BRT = local + 3h
    ///   MDT (UTC-6) → BRT = local + 3h
    ///   PDT (UTC-7) → BRT = local + 4h
    /// </summary>
    private static readonly Dictionary<string, int> StadiumIdToBrasiliaOffset = new(StringComparer.OrdinalIgnoreCase)
    {
        // ═══ EDT (UTC-4) → BRT = local + 1h ═══
        // EUA — Costa Leste
        { "7", 1 },  // Atlanta (Mercedes-Benz Stadium)
        { "8", 1 },  // Miami (Miami Gardens)
        { "9", 1 },  // Boston (Foxborough)
        { "10", 1 }, // Philadelphia
        { "11", 1 }, // New York/New Jersey (East Rutherford)
        // Canadá — Leste
        { "12", 1 }, // Toronto (BMO Field)

        // ═══ CDT (UTC-5) → BRT = local + 2h ═══
        { "4", 2 },  // Dallas (Arlington, Texas)
        { "5", 2 },  // Houston
        { "6", 2 },  // Kansas City

        // ═══ México (UTC-6 fixo, sem horário de verão) → BRT = local + 3h ═══
        { "1", 3 },  // Mexico City (Estadio Azteca)
        { "2", 3 },  // Guadalajara (Zapopan)
        { "3", 3 },  // Monterrey (Guadalupe)

        // ═══ PDT (UTC-7) → BRT = local + 4h ═══
        { "13", 4 }, // Vancouver
        { "14", 4 }, // Seattle
        { "15", 4 }, // San Francisco Bay Area (Santa Clara)
        { "16", 4 }, // Los Angeles (Inglewood)
    };

    private static int GetBrasiliaOffset(string? stadiumId)
    {
        if (stadiumId != null && StadiumIdToBrasiliaOffset.TryGetValue(stadiumId, out var offset))
            return offset;
        return 0;
    }

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

        var score = $"{game.HomeScore} - {game.AwayScore}";

        // Se houve pênaltis, mostra o placar dos pênaltis também
        if (game.HasPenalties)
            score += $" ({game.HomePenaltyScore}-{game.AwayPenaltyScore} pen)";

        return score;
    }

    /// <summary>
    /// Formata a data exibindo no HORÁRIO DE BRASÍLIA (BRT, UTC-3).
    /// </summary>
    /// <param name="localDate">Data/hora local do estádio vinda da API (formato MM/dd/yyyy HH:mm).</param>
    /// <param name="stadiumId">ID do estádio (1-16) para lookup de fuso horário.</param>
    /// <param name="stadiumCities">IGNORADO — mantido apenas para compatibilidade.</param>
    public static string FormatDate(string localDate, string? stadiumId = null,
        Dictionary<string, string>? stadiumCities = null)
    {
        if (!TryParseApiDate(localDate, out var dt))
            return localDate;

        var offsetHours = GetBrasiliaOffset(stadiumId);
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
    /// Extrai apenas a data (sem hora) no formato dd/MM em Brasília.
    /// </summary>
    public static string FormatShortDate(string localDate, string? stadiumId = null,
        Dictionary<string, string>? stadiumCities = null)
    {
        if (!TryParseApiDate(localDate, out var dt))
            return localDate;

        var offsetHours = GetBrasiliaOffset(stadiumId);
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

    /// <summary>
    /// Determina qual time venceu o jogo e retorna o nome em português.
    /// Retorna null se o jogo não foi encerrado ou se é fase de grupos.
    /// 
    /// Para jogos eliminatórios: primeiro verifica o placar normal.
    /// Se empatado, usa o placar dos pênaltis (se disponível).
    /// </summary>
    private static string? GetWinnerName(Models.Game game)
    {
        if (!game.IsFinished)
            return null;

        if (game.Stage == Models.MatchStage.Group)
            return null;

        if (!int.TryParse(game.HomeScore, out var homeScore) ||
            !int.TryParse(game.AwayScore, out var awayScore))
            return null;

        // 1) Venceu no tempo normal ou na prorrogação
        if (homeScore > awayScore)
            return GetHomeName(game);
        if (awayScore > homeScore)
            return GetAwayName(game);

        // 2) Empate — decide nos pênaltis
        if (game.HasPenalties)
        {
            if (game.HomePenaltyScore > game.AwayPenaltyScore)
                return GetHomeName(game);
            if (game.AwayPenaltyScore > game.HomePenaltyScore)
                return GetAwayName(game);
        }

        // Sem dados de pênalti para desempatar
        return null;
    }

    /// <summary>
    /// Retorna o texto de classificação para jogos eliminatórios encerrados.
    /// Ex: "✓ Brasil classificou!", "✓ Argentina é campeã mundial!", "✓ França é 3º lugar!"
    /// </summary>
    public static string GetQualificationText(Models.Game game)
    {
        var winner = GetWinnerName(game);
        if (winner == null)
            return string.Empty;

        var stage = game.Stage;
        if (stage == Models.MatchStage.Final)
            return $"✓ {winner} é campeão mundial!";
        if (stage == Models.MatchStage.ThirdPlace)
            return $"✓ {winner} é 3º lugar!";

        return $"✓ {winner} classificou!";
    }
}
