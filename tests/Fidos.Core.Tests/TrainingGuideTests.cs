using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

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
