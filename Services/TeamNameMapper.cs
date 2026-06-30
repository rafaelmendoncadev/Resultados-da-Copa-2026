namespace Resultados_da_Copa_2026.Services;

public static class TeamNameMapper
{
    private static readonly Dictionary<string, string> PortugueseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Brazil"] = "Brasil",
        ["Argentina"] = "Argentina",
        ["Mexico"] = "México",
        ["United States"] = "Estados Unidos",
        ["USA"] = "Estados Unidos",
        ["Germany"] = "Alemanha",
        ["France"] = "França",
        ["Spain"] = "Espanha",
        ["Portugal"] = "Portugal",
        ["England"] = "Inglaterra",
        ["Netherlands"] = "Holanda",
        ["Belgium"] = "Bélgica",
        ["Italy"] = "Itália",
        ["Croatia"] = "Croácia",
        ["Uruguay"] = "Uruguai",
        ["Colombia"] = "Colômbia",
        ["Japan"] = "Japão",
        ["South Korea"] = "Coreia do Sul",
        ["Morocco"] = "Marrocos",
        ["Senegal"] = "Senegal",
        ["Switzerland"] = "Suíça",
        ["Canada"] = "Canadá",
        ["Australia"] = "Austrália",
        ["Ecuador"] = "Equador",
        ["Paraguay"] = "Paraguai",
        ["Chile"] = "Chile",
        ["Peru"] = "Peru",
        ["Costa Rica"] = "Costa Rica",
        ["Panama"] = "Panamá",
        ["Qatar"] = "Catar",
        ["Saudi Arabia"] = "Arábia Saudita",
        ["Iran"] = "Irã",
        ["Tunisia"] = "Tunísia",
        ["Cameroon"] = "Camarões",
        ["Ghana"] = "Gana",
        ["Nigeria"] = "Nigéria",
        ["Ivory Coast"] = "Costa do Marfim",
        ["Poland"] = "Polônia",
        ["Serbia"] = "Sérvia",
        ["Denmark"] = "Dinamarca",
        ["Sweden"] = "Suécia",
        ["Wales"] = "País de Gales",
        ["Scotland"] = "Escócia",
        ["Turkey"] = "Turquia",
        ["Ukraine"] = "Ucrânia",
        ["Czech Republic"] = "República Tcheca",
        ["Austria"] = "Áustria",
        ["Hungary"] = "Hungria",
        ["Romania"] = "Romênia",
        ["South Africa"] = "África do Sul",
        ["New Zealand"] = "Nova Zelândia",
        ["Bosnia and Herzegovina"] = "Bósnia e Herzegovina",
        ["Bosnia & Herzegovina"] = "Bósnia e Herzegovina",
    };

    public static string ToPortuguese(string? englishName)
    {
        if (string.IsNullOrWhiteSpace(englishName))
            return "A definir";

        return PortugueseNames.TryGetValue(englishName, out var pt) ? pt : englishName;
    }

    public static string TranslateKnockoutLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "A definir";

        var result = label;
        result = result.Replace("Winner Group", "Vencedor Grupo", StringComparison.OrdinalIgnoreCase);
        result = result.Replace("Runner-up Group", "2º Grupo", StringComparison.OrdinalIgnoreCase);
        result = result.Replace("Winner Match", "Vencedor Jogo", StringComparison.OrdinalIgnoreCase);
        result = result.Replace("Loser Match", "Perdedor Jogo", StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
