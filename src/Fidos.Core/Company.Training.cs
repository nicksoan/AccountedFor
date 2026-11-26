using System.Text.Json.Serialization;
using Fidos.Core.Training;

namespace Fidos.Core;

public sealed partial class Company
{
    public bool IsLevelUnlocked(int number) => number <= 1 || CompletedLevels.ContainsKey(number - 1);

    public bool IsLevelComplete(int number) => CompletedLevels.ContainsKey(number);

    /// <summary>The lowest level not yet completed, or null when training is finished.</summary>
    [JsonIgnore]
    public Level? CurrentLevel => LevelCatalog.All.FirstOrDefault(l => !IsLevelComplete(l.Number));

    [JsonIgnore]
    public int Score => LevelCatalog.All.Where(l => IsLevelComplete(l.Number)).Sum(l => l.Points) + Drill.Correct * 5;

    [JsonIgnore]
    public string Rank => CompletedLevels.Count switch
    {
        0 => "TRAINEE",
        <= 2 => "JUNIOR CLERK",
        <= 4 => "LEDGER CLERK",
        <= 6 => "BOOKKEEPER",
        <= 8 => "ASSISTANT ACCOUNTANT",
        <= 10 => "ACCOUNTANT",
        <= 12 => "FINANCIAL CONTROLLER",
        _ => "CHIEF FINANCIAL OFFICER",
    };

    /// <summary>Completes the current level if all of its objectives are now met.</summary>
    public Level? CheckLevelProgress()
    {
        var level = CurrentLevel;
        if (level is null || !level.Objectives.All(o => o.IsMet(this)))
        {
            return null;
        }

        CompletedLevels[level.Number] = DateTime.UtcNow;
        return level;
    }

    [JsonIgnore]
    public DrillQuestion CurrentDrillQuestion => DrillCatalog.Get(Drill.CurrentQuestion);

    /// <summary>Checks an answer to the current drill question and moves on to a different random question.</summary>
    public DrillResult AnswerDrill(string? debitCode, string? creditCode, Random? random = null)
    {
        var question = CurrentDrillQuestion;
        var debitOk = string.Equals(debitCode?.Trim(), question.DebitCode, StringComparison.Ordinal);
        var creditOk = string.Equals(creditCode?.Trim(), question.CreditCode, StringComparison.Ordinal);
        var correct = debitOk && creditOk;

        Drill.Attempts++;
        if (correct)
        {
            Drill.Correct++;
            Drill.Streak++;
            Drill.BestStreak = Math.Max(Drill.BestStreak, Drill.Streak);
        }
        else
        {
            Drill.Streak = 0;
        }

        var others = DrillCatalog.All.Where(q => q.Id != question.Id).ToList();
        Drill.CurrentQuestion = others[(random ?? Random.Shared).Next(others.Count)].Id;
        return new DrillResult(question, correct, debitOk, creditOk);
    }
}
