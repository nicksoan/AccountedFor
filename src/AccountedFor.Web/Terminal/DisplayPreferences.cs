namespace AccountedFor.Web.Terminal;

public static class DisplayPreferences
{
    public const string DisableCrtCookie = "FIDOS.NOCRT";
    public const string FontColorCookie = "FIDOS.COLOR";

    public static bool IsSupportedFontColor(string? color) =>
        color is "green" or "amber" or "white" or "cyan";

    public static string FontColorOrDefault(string? color) =>
        IsSupportedFontColor(color) ? color! : "green";
}
