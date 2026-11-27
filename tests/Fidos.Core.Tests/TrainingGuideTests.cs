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

    [Theory]
    [InlineData("LEVELS", "/Training")]
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
}
