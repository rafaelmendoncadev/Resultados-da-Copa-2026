namespace Resultados_da_Copa_2026.Models;

public enum MatchStage
{
    Group,
    RoundOf32,
    RoundOf16,
    QuarterFinal,
    SemiFinal,
    ThirdPlace,
    Final,
    Unknown
}

public static class MatchStageExtensions
{
    public static MatchStage FromApiType(string? type) => type?.ToLowerInvariant() switch
    {
        "group" => MatchStage.Group,
        "r32" => MatchStage.RoundOf32,
        "r16" => MatchStage.RoundOf16,
        "qf" => MatchStage.QuarterFinal,
        "sf" => MatchStage.SemiFinal,
        "3rd" => MatchStage.ThirdPlace,
        "final" => MatchStage.Final,
        _ => MatchStage.Unknown
    };

    public static string ToDisplayName(this MatchStage stage) => stage switch
    {
        MatchStage.Group => "Fase de grupos",
        MatchStage.RoundOf32 => "32 avos de final",
        MatchStage.RoundOf16 => "Oitavas de final",
        MatchStage.QuarterFinal => "Quartas de final",
        MatchStage.SemiFinal => "Semifinal",
        MatchStage.ThirdPlace => "Disputa de 3º lugar",
        MatchStage.Final => "Final",
        _ => "Desconhecido"
    };

    public static int SortOrder(this MatchStage stage) => stage switch
    {
        MatchStage.Group => 0,
        MatchStage.RoundOf32 => 1,
        MatchStage.RoundOf16 => 2,
        MatchStage.QuarterFinal => 3,
        MatchStage.SemiFinal => 4,
        MatchStage.ThirdPlace => 5,
        MatchStage.Final => 6,
        _ => 99
    };
}
