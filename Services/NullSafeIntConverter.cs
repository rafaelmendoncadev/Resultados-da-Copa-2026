using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Services;

/// <summary>
/// Converte campos <c>int?</c> tolerando as variações que a API worldcup26.ir envia:
/// JSON <c>null</c>, a string literal <c>"null"</c>, string vazia ou número (em JSON ou
/// como string numérica). Qualquer valor não reconhecível vira <c>null</c> em vez de
/// lançar <see cref="JsonException"/>, o que evita que um único jogo inválido derrube
/// todo o parse da lista de jogos.
/// </summary>
public sealed class NullSafeIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.TryGetInt32(out var n) ? n : null,
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    private static int? ParseString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (raw.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        return int.TryParse(raw, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }
}
