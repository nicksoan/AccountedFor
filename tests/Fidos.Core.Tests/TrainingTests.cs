using System.Text.Json;
using Fidos.Core;
using Fidos.Core.Training;

namespace Fidos.Core.Tests;

public class TrainingTests
{
    private static readonly DateOnly Day = new(2026, 2, 1);

    [Fact]
    public void Levels_AreSequentialAndNumberedUniquely()
    {
        Assert.Equal(Enumerable.Range(1, LevelCatalog.All.Count), LevelCatalog.All.Select(l => l.Number));
        Assert.All(LevelCatalog.All, l => Assert.NotEmpty(l.Objectives));
    }

    [Fact]
    public void CheckLevelProgress_CompletesOnlyCurrentLevel()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "Capital", [new("1000", 100, 0), new("3000", 0, 100)]);

        Assert.Null(company.CheckLevelProgress());
        Assert.False(company.IsLevelUnlocked(3));

        company.Mark(Milestones.ChartOfAccounts);
        company.Mark(Milestones.AccountInquiry);

        Assert.Equal(1, company.CheckLevelProgress()?.Number);
        Assert.True(company.IsLevelUnlocked(2));
        Assert.Equal(2, company.CurrentLevel?.Number);
        Assert.Equal(50, company.Score);
    }

    [Fact]
    public void Level3_IsMetByCapitalJournal()
    {
        var company = Company.CreateNew();
        var level = LevelCatalog.Get(3)!;
        Assert.False(level.Objectives.All(o => o.IsMet(company)));

        company.PostJournal(Day, "Capital", [new("1000", 100, 0), new("3000", 0, 100)]);

        Assert.True(level.Objectives.All(o => o.IsMet(company)));
    }

    [Fact]
    public void AnswerDrill_ScoresAndMovesOn()
    {
        var company = Company.CreateNew();
        var question = company.CurrentDrillQuestion;

        var result = company.AnswerDrill(question.DebitCode, question.CreditCode, new Random(1));

        Assert.True(result.IsCorrect);
        Assert.Equal(1, company.Drill.Correct);
        Assert.Equal(1, company.Drill.Streak);
        Assert.NotEqual(question.Id, company.Drill.CurrentQuestion);

        var wrong = company.AnswerDrill(company.CurrentDrillQuestion.CreditCode, company.CurrentDrillQuestion.DebitCode);
        Assert.False(wrong.IsCorrect);
        Assert.Equal(0, company.Drill.Streak);
        Assert.Equal(1, company.Drill.BestStreak);
    }

    [Fact]
    public void Practice_PostsBothSidesInSeparateBooksAndSurvivesReload()
    {
        var company = Company.CreateNew();
        company.CompletedLevels[1] = DateTime.UtcNow;
        var level = LevelCatalog.Get(2)!;
        company.PostJournal(Day, "Original sandbox entry", [new("1000", 400, 0), new("3000", 0, 400)]);

        for (var i = 0; i < PracticeCatalog.All.Count; i++)
        {
            var mission = PracticeCatalog.All[i];
            Assert.False(level.Objectives[i].IsMet(company));
            var entry = company.PostPracticeEntry(i, mission.DebitCode, mission.Amount, mission.CreditCode, mission.Amount);
            Assert.Equal(i + 1, entry.Number);
            Assert.Equal(mission.Amount, entry.TotalDebits);
            Assert.Equal(mission.Amount, entry.TotalCredits);
            Assert.Contains(entry.Lines, l => l.AccountCode == mission.DebitCode && l.Debit == mission.Amount);
            Assert.Contains(entry.Lines, l => l.AccountCode == mission.CreditCode && l.Credit == mission.Amount);
            Assert.True(level.Objectives[i].IsMet(company));
            if (i < PracticeCatalog.All.Count - 1)
            {
                Assert.Null(company.CheckLevelProgress());
            }
            company = JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(company))!;
        }

        Assert.Single(company.Journals);
        Assert.Equal(400, company.Balance("1000"));
        Assert.Equal(115, PracticeCatalog.OpenBooks(company.PracticeMissionsCompleted).Balance("1000"));
        Assert.Equal(-25, PracticeCatalog.OpenBooks(company.PracticeMissionsCompleted).Balance("4000"));
        Assert.Equal(0, company.Drill.Correct);
        Assert.Equal(2, company.CheckLevelProgress()?.Number);
        Assert.True(company.IsLevelUnlocked(3));
        Assert.Throws<BookkeepingException>(() => company.PostPracticeEntry(0, "1000", 100, "3000", 100));
    }

    [Fact]
    public void Practice_RejectsLockedOrOutOfOrderMissions()
    {
        var company = Company.CreateNew();
        Assert.Throws<BookkeepingException>(() => company.PostPracticeEntry(0, "1000", 100, "3000", 100));
        company.CompletedLevels[1] = DateTime.UtcNow;
        Assert.Throws<BookkeepingException>(() => company.PostPracticeEntry(1, "1000", 25, "4000", 25));
        Assert.Equal(0, company.PracticeMissionsCompleted);
    }

    [Theory]
    [InlineData("3000", 100, "1000", 100, "debit (left)")]
    [InlineData("1000", 100, "4000", 100, "credit (right)")]
    [InlineData("1000", 100, "3000", 90, "doesn't balance")]
    [InlineData("1000", 90, "3000", 90, "this transaction is for")]
    public void Practice_InvalidEntryGivesFeedbackWithoutPosting(string dr, int debit, string cr, int credit, string feedback)
    {
        var company = Company.CreateNew();
        company.CompletedLevels[1] = DateTime.UtcNow;
        var error = Assert.Throws<BookkeepingException>(() => company.PostPracticeEntry(0, dr, debit, cr, credit));
        Assert.Contains(feedback, error.Message);
        Assert.Equal(0, company.PracticeMissionsCompleted);
        Assert.Empty(PracticeCatalog.OpenBooks(company.PracticeMissionsCompleted).Journals);
        Assert.Empty(company.Journals);
    }

    [Fact]
    public void Level2_RequiresPracticeButRetainsCompletedLevels()
    {
        var company = Company.CreateNew();
        var level = LevelCatalog.Get(2)!;
        company.Drill.Correct = 5;
        Assert.All(level.Objectives, o => Assert.False(o.IsMet(company)));

        company.Drill.Correct = 0;
        company.CompletedLevels[2] = DateTime.UtcNow;
        Assert.All(level.Objectives, o => Assert.True(o.IsMet(company)));
    }

    [Fact]
    public void DrillQuestions_UseSeededAccounts()
    {
        var company = Company.CreateNew();
        Assert.All(DrillCatalog.All, q =>
        {
            Assert.NotNull(company.FindAccount(q.DebitCode));
            Assert.NotNull(company.FindAccount(q.CreditCode));
        });
    }

    [Fact]
    public void Company_RoundTripsThroughJson()
    {
        var company = Company.CreateNew("bob");
        var customer = company.AddCustomer("Acme");
        company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 20);
        company.PostCustomerReceipt(customer.Id, Day, 50);
        company.Mark(Milestones.TrialBalance);
        company.CompletedLevels[1] = DateTime.UtcNow;

        var copy = JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(company))!;

        Assert.Equal(70, copy.CustomerBalance(customer.Id));
        Assert.Equal(70, copy.SalesInvoices[0].Outstanding);
        Assert.True(copy.HasMilestone(Milestones.TrialBalance));
        Assert.True(copy.IsLevelComplete(1));
        Assert.Equal("C0002", copy.AddCustomer("Next").Id);
        Assert.Equal(company.Journals.Count, copy.Journals.Count);
    }
}
