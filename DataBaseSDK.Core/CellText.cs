using System.Globalization;

namespace DataBaseSDK.Core;

/// <summary>
/// Cómo se convierte el valor de una celda en lo que se ve (y se compara). Una sola definición para la grilla de
/// resultados, el CSV y la edición: la edición compara el valor ACTUAL de la base con el que el usuario vio al
/// consultar, y para que esa comparación sea justa los dos lados tienen que pasar por exactamente este formato.
/// </summary>
public static class CellText
{
    private const int MaxCellChars = 4000;

    /// <summary>Valor del driver → null, texto, número, bool o DateTime (binarios y textos largos ya convertidos/recortados).</summary>
    public static object? Normalize(object? value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                return null;

            case string text:
                return Truncate(text);

            case byte[] bytes:
                var hex = Convert.ToHexString(bytes.AsSpan(0, Math.Min(bytes.Length, 32)));
                return $"0x{hex}{(bytes.Length > 32 ? $"… ({bytes.Length} bytes)" : string.Empty)}";

            case Guid guid:
                return guid.ToString();

            case TimeSpan time:
                return time.ToString();

            case DateTimeOffset offset:
                return offset.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

            case int or long or short or byte or sbyte or ushort or uint or ulong or decimal or double or float or bool or DateTime:
                return value;

            default:
                return Truncate(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    public static string Truncate(string text) =>
        text.Length > MaxCellChars ? text[..MaxCellChars] + "…" : text;

    /// <summary>true si este texto salió de <see cref="Truncate"/> recortado: editarlo y guardarlo destruiría el resto del valor.</summary>
    public static bool IsTruncated(string? text) =>
        text != null && text.Length == MaxCellChars + 1 && text[^1] == '…';

    /// <summary>El texto que se muestra en la grilla; null para NULL.</summary>
    public static string? Format(object? value) => value switch
    {
        null => null,
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        var other => other.ToString()
    };
}
