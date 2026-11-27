using Fidos.Core;
using Fidos.Core.Training;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Terminal;

/// <summary>Base class for every FIDOS screen that needs a logged-on sandbox.</summary>
public abstract class TerminalPageModel(SandboxStore store) : PageModel
{
    public const string CookieName = "FIDOS.SANDBOX";
    public const string RememberedCookieName = "FIDOS.REMEMBERED";

    public static void SetSandboxCookie(HttpContext context, string name, string id) =>
        context.Response.Cookies.Append(name, id, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
        });

    protected SandboxStore Store => store;

    public Company Company { get; private set; } = null!;

    public string SandboxId { get; private set; } = "";

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var id = Request.Cookies[CookieName];
        var company = store.Find(id);
        if (company is null)
        {
            context.Result = RedirectToPage("/Index");
            return;
        }

        SandboxId = id!.ToLowerInvariant();
        Company = company;
        ViewData["Company"] = company;

        if (Request.Cookies.TryGetValue(TrainingGuide.CookieName, out var selection) &&
            TrainingGuide.LegacySelectedLevel(company, SandboxId, selection) is { } selected)
        {
            if (company.SelectedTrainingLevel is null)
            {
                TryUpdate(c => c.SelectedTrainingLevel = selected.Number);
            }

            Response.Cookies.Delete(TrainingGuide.CookieName);
        }
    }

    /// <summary>Runs a bookkeeping operation, saving the sandbox. Bookkeeping errors are shown on the message line.</summary>
    protected bool TryUpdate<T>(Func<Company, T> action, out T result)
    {
        try
        {
            result = store.Update(SandboxId, Company, action, out var level);
            if (level is not null)
            {
                TempData["LevelUp"] = $"*** LEVEL {level.Number} COMPLETE: {level.Title} +{level.Points} PTS *** RANK: {Company.Rank}";
            }

            return true;
        }
        catch (BookkeepingException ex)
        {
            ShowError(ex.Message);
            result = default!;
            return false;
        }
    }

    /// <summary>
    /// Completes any levels whose objectives were already met (e.g. work done out of order),
    /// since each posting only completes the current level.
    /// </summary>
    protected void CatchUpLevels()
    {
        var completed = new List<Level>();
        for (var i = 0; i < LevelCatalog.All.Count; i++)
        {
            var current = Company.CurrentLevel;
            if (current is null || !current.Objectives.All(o => o.IsMet(Company)))
            {
                break;
            }

            store.Update(SandboxId, Company, _ => true, out var level);
            if (level is null)
            {
                break;
            }

            completed.Add(level);
        }

        if (completed.Count > 0)
        {
            TempData["LevelUp"] = string.Join("  ", completed.Select(l => $"*** LEVEL {l.Number} COMPLETE: {l.Title} +{l.Points} PTS ***"))
                + $" RANK: {Company.Rank}";
        }
    }

    protected bool TryUpdate(Action<Company> action) => TryUpdate(c => { action(c); return true; }, out _);

    /// <summary>Records that a screen was visited, for training objectives.</summary>
    protected void Milestone(string milestone)
    {
        if (!Company.HasMilestone(milestone))
        {
            TryUpdate(c => c.Mark(milestone));
        }
    }

    protected void ShowError(string message) => ViewData["Error"] = message;

    protected void Flash(string message) => TempData["Message"] = message;

    protected bool TryParseDate(string? text, out DateOnly date, string field = "DATE")
    {
        if (Fmt.TryParseDate(text, out date))
        {
            return true;
        }

        ShowError($"{field} MUST BE IN THE FORMAT YYYY-MM-DD");
        return false;
    }

    protected bool TryParseAmount(string? text, out decimal amount, string field)
    {
        if (Fmt.TryParseAmount(text, out amount))
        {
            return true;
        }

        ShowError($"{field}: '{text}' IS NOT A VALID AMOUNT");
        return false;
    }
}
