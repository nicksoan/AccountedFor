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
}
