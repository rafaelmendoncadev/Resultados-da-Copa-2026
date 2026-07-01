using System.Globalization;

namespace Resultados_da_Copa_2026.Services;

public static class GameDisplayHelper
{
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

    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);
    private static readonly TimeSpan ApiOffset = TimeSpan.FromHours(-5);

    public static string FormatDate(string localDate)
    {
        if (TryParseAndConvertToBrasilia(localDate, out var dt))
            return dt.ToString("dd/MM/yyyy HH:mm");
        return localDate;
    }

    public static string FormatShortDate(string localDate)
    {
        if (TryParseAndConvertToBrasilia(localDate, out var dt))
            return dt.ToString("dd/MM");
        return localDate;
    }

    private static bool TryParseAndConvertToBrasilia(string localDate, out DateTime result)
    {
        if (!TryParseApiDate(localDate, out var parsed))
        {
            result = default;
            return false;
        }

        var utc = parsed - ApiOffset;
        result = utc + BrasiliaOffset;
        return true;
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
