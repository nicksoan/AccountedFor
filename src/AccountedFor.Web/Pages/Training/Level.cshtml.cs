using AccountedFor.Web.Terminal;
using Fidos.Core.Training;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Training;

public class LevelModel(SandboxStore store) : TerminalPageModel(store)
{
    public Level Level { get; private set; } = null!;

    public IActionResult OnGet(int number)
    {
        var level = LevelCatalog.All.FirstOrDefault(l => l.Number == number);
        if (level is null)
        {
            return Redirect("/Training");
        }

        CatchUpLevels();
        Level = level;
        if (Company.IsLevelUnlocked(number))
        {
            Response.Cookies.Append(TrainingGuide.CookieName, TrainingGuide.Selection(SandboxId, number), new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
            });
            ViewData["GuideLevel"] = level;
        }

        return Page();
    }
}
