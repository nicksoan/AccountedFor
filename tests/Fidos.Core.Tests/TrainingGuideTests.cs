using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;
using System.Text.Json;

namespace Fidos.Core.Tests;

public class TrainingGuideTests
{
    [Fact]
    public void Selection_TracksAnUnlockedLessonAfterCompletion()
    {
        var company = Company.CreateNew();
        var selection = TrainingGuide.Selection("sandbox", 1);
        company.CompletedLevels[1] = DateTime.UtcNow;

        Assert.Equal(2, company.CurrentLevel?.Number);
        Assert.Equal(1, TrainingGuide.SelectedLevel(company, "sandbox", selection)?.Number);
    }

    [Fact]
    public void SavedSelection_TracksProgressAcrossSandboxRoundTrip()
    {
        var company = Company.CreateNew();
        company.SelectedTrainingLevel = 1;
        company.Mark(Milestones.ChartOfAccounts);

        var copy = JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(company))!;
        var selected = TrainingGuide.SelectedLevel(copy, "sandbox", null)!;

        Assert.Equal(1, selected.Number);
        Assert.True(selected.Objectives[0].IsMet(copy));
        Assert.False(selected.Objectives[1].IsMet(copy));

        copy.Mark(Milestones.AccountInquiry);
        copy.CheckLevelProgress();
        copy = JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(copy))!;
        selected = TrainingGuide.SelectedLevel(copy, "sandbox", null)!;

        Assert.Equal(1, selected.Number);
        Assert.All(selected.Objectives, objective => Assert.True(objective.IsMet(copy)));
        Assert.Equal(2, copy.CurrentLevel?.Number);
    }

    [Fact]
    public void SavedSelection_TakesPrecedenceOverLegacyCookie()
    {
        var company = Company.CreateNew();
        company.CompletedLevels[1] = DateTime.UtcNow;
        company.SelectedTrainingLevel = 2;

        Assert.Equal(2, TrainingGuide.SelectedLevel(company, "sandbox", "sandbox:1")?.Number);
    }

    [Fact]
    public void Selection_AfterAllLessonsAreComplete_CanStillReviewASelectedLesson()
    {
        var company = Company.CreateNew();
        foreach (var level in LevelCatalog.All)
        {
            company.CompletedLevels[level.Number] = DateTime.UtcNow;
        }

        Assert.Null(TrainingGuide.SelectedLevel(company, "sandbox", null));
        company.SelectedTrainingLevel = 1;
        Assert.Equal(1, TrainingGuide.SelectedLevel(company, "sandbox", null)?.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(999)]
    public void InvalidSavedSelection_FallsBackToCurrentLevel(int number)
    {
        var company = Company.CreateNew();
        company.SelectedTrainingLevel = number;

        Assert.Equal(1, TrainingGuide.SelectedLevel(company, "sandbox", null)?.Number);
    }

    [Fact]
    public void LegacySelection_IsOnlyAcceptedForItsOwnSandboxAndUnlockedLessons()
    {
        var company = Company.CreateNew();

        Assert.Equal(1, TrainingGuide.LegacySelectedLevel(company, "sandbox", "sandbox:1")?.Number);
        Assert.Null(TrainingGuide.LegacySelectedLevel(company, "sandbox", "other:1"));
        Assert.Null(TrainingGuide.LegacySelectedLevel(company, "sandbox", "sandbox:2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("other:1")]
    [InlineData("sandbox:2")]
    [InlineData("sandbox:999")]
    [InlineData("sandbox:not-a-number")]
    public void Selection_FallsBackToCurrentLevelForInvalidOrLockedLessons(string? selection)
    {
        var company = Company.CreateNew();

        Assert.Equal(1, TrainingGuide.SelectedLevel(company, "sandbox", selection)?.Number);
    }

    [Fact]
    public void EveryObjective_HasALocalDestination()
    {
        foreach (var level in LevelCatalog.All)
        {
            for (var i = 0; i < level.Objectives.Count; i++)
            {
                var url = TrainingGuide.ObjectiveUrl(level.Number, i);
                Assert.NotNull(url);
                Assert.Contains(url, Navigation.Menus.SelectMany(m => m.Options).Select(o => o.Url));
            }

            Assert.Null(TrainingGuide.ObjectiveUrl(level.Number, level.Objectives.Count));
        }
        Assert.Null(TrainingGuide.ObjectiveUrl(0, 0));
    }

    [Fact]
    public void GuidedScenarios_ProvideOrderedInstructionsForEveryObjective()
    {
        var scenarioLevels = LevelCatalog.All.Where(level => level.Number != 2);
        foreach (var level in scenarioLevels)
        {
            Assert.Equal(level.Objectives.Count, level.Scenarios.Count);

            for (var i = 0; i < level.Scenarios.Count; i++)
            {
                var scenario = level.Scenarios[i];

                Assert.Equal(i, scenario.ObjectiveIndex);
                Assert.NotEmpty(scenario.Situation);
                Assert.NotEmpty(scenario.Teaching);
                Assert.NotEmpty(scenario.Instructions);

                var url = TrainingGuide.ObjectiveUrl(level.Number, scenario.ObjectiveIndex);
                Assert.NotNull(url);
                Assert.Contains(url, Navigation.Menus.SelectMany(menu => menu.Options).Select(option => option.Url));
            }
        }
    }

    [Fact]
    public void DeadClicObjectives_OpenThePracticeBooks()
    {
        var level = LevelCatalog.Get(2)!;

        Assert.Equal(PracticeCatalog.All.Count, level.Objectives.Count);
        for (var i = 0; i < level.Objectives.Count; i++)
        {
            Assert.Equal("/Training/Practice", TrainingGuide.ObjectiveUrl(level.Number, i));
        }
    }

    [Fact]
    public void DeadClicCutscene_UsesTheImmediateBankSaleFromPractice()
    {
        var sale = PracticeCatalog.All[1];

        Assert.Equal("First sale", sale.Title);
        Assert.Contains("bank transfer right away", sale.Situation);
        Assert.Contains("Ignore VAT", sale.Situation);
        Assert.Equal(SystemAccounts.Bank, sale.DebitCode);
        Assert.Equal("4000", sale.CreditCode);
        Assert.Equal(25m, sale.Amount);
    }

    [Theory]
    [InlineData("LEVELS", "/Training")]
    [InlineData("PRACTICE", "/Training/Practice")]
    [InlineData("HELP", "/Help")]
    public void GuideCommands_OpenTheirAdvertisedDestinations(string command, string destination)
    {
        Assert.Equal(destination, Navigation.Resolve(command, Navigation.MainMenu));
    }

    [Fact]
    public void GuideMenuOptions_LeadToLessonsAndAccountingFundamentals()
    {
        var main = Navigation.FindMenu("MAIN")!;
        var tutorial = Assert.Single(main.Options, option => option.Key == "5");
        var fundamentals = Assert.Single(main.Options, option => option.Key == "6");

        Assert.Equal("TUTORIAL", tutorial.Label);
        Assert.Equal("ACCOUNTING FUNDAMENTALS", fundamentals.Label);
        Assert.Equal(tutorial.Url, Navigation.Resolve("5", main.Url));
        Assert.Equal("/Training", Navigation.Resolve("1", tutorial.Url));
        Assert.Equal("/Help", Navigation.Resolve("6", main.Url));
        Assert.Equal("/Help", fundamentals.Url);
    }

    [Fact]
    public void TrainingLevelNumbers_OpenTheirMatchingLessons()
    {
        foreach (var level in LevelCatalog.All)
        {
            Assert.Equal($"/Training/Level/{level.Number}", Navigation.Resolve(level.Number.ToString(), "/Training"));
        }

        Assert.Null(Navigation.Resolve("0", "/Training"));
        Assert.Null(Navigation.Resolve("999", "/Training"));
    }
}
