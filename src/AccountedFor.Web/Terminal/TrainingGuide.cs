using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Terminal;

public static class TrainingGuide
{
    public const string CookieName = "FIDOS.LESSON";

    private static readonly string[][] ObjectiveUrls =
    [
        ["/Gl/Accounts", "/Gl/Accounts"],
        ["/Training/Drill", "/Training/Drill", "/Training/Drill", "/Training/Drill", "/Training/Drill", "/Training/Drill"],
        ["/Gl/Journal"],
        ["/Gl/NewAccount", "/Gl/Journal"],
        ["/ar/parties/new", "/ar/invoice"],
        ["/ar/settle", "/ar/settle"],
        ["/ap/parties/new", "/ap/invoice"],
        ["/ap/settle", "/ap/settle"],
        ["/Reports/TrialBalance", "/Reports/Reconcile"],
        ["/Gl/Journals"],
        ["/Gl/Journal", "/Gl/Journal"],
        ["/Reports/ProfitLoss", "/Reports/BalanceSheet", "/ar/aged"],
        ["/Gl/YearEnd"],
    ];

    public static string? ObjectiveUrl(int levelNumber, int objectiveIndex) =>
        levelNumber > 0 && levelNumber <= ObjectiveUrls.Length &&
        objectiveIndex >= 0 && objectiveIndex < ObjectiveUrls[levelNumber - 1].Length
            ? ObjectiveUrls[levelNumber - 1][objectiveIndex]
            : null;

    public static string Selection(string sandboxId, int levelNumber) => $"{sandboxId}:{levelNumber}";

    public static Level? SelectedLevel(Company company, string? sandboxId, string? selection)
    {
        if (company.SelectedTrainingLevel is int number &&
            LevelCatalog.Get(number) is { } level && company.IsLevelUnlocked(number))
        {
            return level;
        }

        return LegacySelectedLevel(company, sandboxId, selection) ?? company.CurrentLevel;
    }

    public static Level? LegacySelectedLevel(Company company, string? sandboxId, string? selection)
    {
        var parts = selection?.Split(':');
        if (parts is { Length: 2 } && !string.IsNullOrEmpty(sandboxId) &&
            string.Equals(parts[0], sandboxId, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(parts[1], out var number) &&
            LevelCatalog.Get(number) is { } level && company.IsLevelUnlocked(number))
        {
            return level;
        }

        return null;
    }
}
