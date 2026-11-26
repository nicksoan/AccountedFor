using System.Globalization;

namespace AccountedFor.Web.Terminal;

/// <summary>Fixed-format display and parsing helpers for terminal screens.</summary>
public static class Fmt
{
    public const string DateFormat = "yyyy-MM-dd";

    public static string Amt(decimal value) => value.ToString("#,##0.00;(#,##0.00)", CultureInfo.InvariantCulture);

    public static string AmtOrBlank(decimal value) => value == 0 ? "" : Amt(value);

    public static string Date(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string Jnl(int number) => number.ToString("000000", CultureInfo.InvariantCulture);

    /// <summary>Parses an amount such as "1,234.50". Blank means zero.</summary>
    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return decimal.TryParse(
            text.Trim().Replace(",", "", StringComparison.Ordinal),
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out amount);
    }

    public static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
