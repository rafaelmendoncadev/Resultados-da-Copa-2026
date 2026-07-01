using System.Globalization;
using System.Text.RegularExpressions;

namespace Resultados_da_Copa_2026.Services;

public static class GameDisplayHelper
{
    // Horário de Brasília (UTC-3). A API openfootball não tem horário de verão
    // nos países-sede em junho, e Brasília também não, então offset fixo é suficiente.
    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);

    // Extrai o offset (ex.: "13:00 UTC-6" -> -6h) que vem embutido na própria string
    // "time" da API openfootball. Cada jogo carrega o fuso real do seu estádio.
    private static readonly Regex TimeZoneOffsetRegex =
        new(@"UTC([+-])(\d{1,2})(?::?(\d{2}))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Offset UTC de cada estádio da Copa 2026 (em junho/julho, já considerando o
    // horário de verão dos EUA/Canadá — o México não tem DST). Usado como fallback
    // quando a API primária (worldcup26.ir) envia "local_date" sem sufixo de fuso,
    // caso em que o horário vem no fuso local do próprio estádio.
    private static readonly Dictionary<string, TimeSpan> StadiumOffsets = new()
    {
        // México — UTC-6
        ["1"] = TimeSpan.FromHours(-6),  // Cidade do México
        ["2"] = TimeSpan.FromHours(-6),  // Guadalajara
        ["3"] = TimeSpan.FromHours(-6),  // Monterrey
        // UTC-5 (Dallas, Houston, Kansas City)
        ["4"] = TimeSpan.FromHours(-5),
        ["5"] = TimeSpan.FromHours(-5),
        ["6"] = TimeSpan.FromHours(-5),
        // UTC-4 (Atlanta, Miami, Boston, Filadélfia, NY/NJ, Toronto)
        ["7"] = TimeSpan.FromHours(-4),
        ["8"] = TimeSpan.FromHours(-4),
        ["9"] = TimeSpan.FromHours(-4),
        ["10"] = TimeSpan.FromHours(-4),
        ["11"] = TimeSpan.FromHours(-4),
        ["12"] = TimeSpan.FromHours(-4),
        // UTC-7 (Vancouver, Seattle, São Francisco, Los Angeles)
        ["13"] = TimeSpan.FromHours(-7),
        ["14"] = TimeSpan.FromHours(-7),
        ["15"] = TimeSpan.FromHours(-7),
        ["16"] = TimeSpan.FromHours(-7),
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

        var score = $"{game.HomeScore} - {game.AwayScore}";

        if (game.HasPenalties)
            score += $" ({game.HomePenaltyScore}-{game.AwayPenaltyScore} pen)";

        return score;
    }

    public static string FormatDate(string localDate, string? stadiumId = null)
    {
        if (TryParseAndConvertToBrasilia(localDate, stadiumId, out var dt))
            return dt.ToString("dd/MM/yyyy HH:mm");
        return localDate;
    }

    public static string FormatShortDate(string localDate, string? stadiumId = null)
    {
        if (TryParseAndConvertToBrasilia(localDate, stadiumId, out var dt))
            return dt.ToString("dd/MM");
        return localDate;
    }

    private static bool TryParseAndConvertToBrasilia(string localDate, string? stadiumId, out DateTime result)
    {
        // A string pode vir como "yyyy-MM-dd HH:mm UTC-6" (openfootball, com fuso) ou
        // "MM/dd/yyyy HH:mm" (worldcup26.ir, sem fuso — horário local do estádio).
        var apiOffset = ExtractOffset(localDate, out var withoutTz);
        if (!TryParseApiDate(withoutTz, out var parsed))
        {
            result = default;
            return false;
        }

        // Sem fuso na string: tenta o offset do estádio (fallback da API primária).
        // Se nem isso existir, preserva o horário original.
        if (apiOffset == null)
        {
            if (stadiumId != null && StadiumOffsets.TryGetValue(stadiumId, out var stadiumOffset))
                apiOffset = stadiumOffset;
            else
            {
                result = parsed;
                return true;
            }
        }

        var utc = parsed - apiOffset.Value;
        result = utc + BrasiliaOffset;
        return true;
    }

    /// <summary>
    /// Extrai o offset (ex.: "UTC-6" -> -6h) embutido na string de hora da API
    /// openfootball. Retorna null quando não há sufixo de fuso.
    /// </summary>
    private static TimeSpan? ExtractOffset(string localDate, out string withoutTz)
    {
        var match = TimeZoneOffsetRegex.Match(localDate);
        if (!match.Success)
        {
            withoutTz = localDate;
            return null;
        }

        var hours = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var minutes = match.Groups[3].Success
            ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)
            : 0;
        var total = new TimeSpan(hours, minutes, 0);
        if (match.Groups[1].Value == "-")
            total = total.Negate();

        withoutTz = localDate[..match.Index].Trim();
        return total;
    }

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

    private static string? GetWinnerName(Models.Game game)
    {
        if (!game.IsFinished)
            return null;

        if (game.Stage == Models.MatchStage.Group)
            return null;

        if (!int.TryParse(game.HomeScore, out var homeScore) ||
            !int.TryParse(game.AwayScore, out var awayScore))
            return null;

        if (homeScore > awayScore)
            return GetHomeName(game);
        if (awayScore > homeScore)
            return GetAwayName(game);

        if (game.HasPenalties)
        {
            if (game.HomePenaltyScore > game.AwayPenaltyScore)
                return GetHomeName(game);
            if (game.AwayPenaltyScore > game.HomePenaltyScore)
                return GetAwayName(game);
        }

        return null;
    }

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
