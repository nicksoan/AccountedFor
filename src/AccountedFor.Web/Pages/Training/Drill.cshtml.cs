using AccountedFor.Web.Terminal;
using Fidos.Core.Training;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Training;

public class DrillModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public string? Debit { get; set; }

    [BindProperty]
    public string? Credit { get; set; }

    [BindProperty]
    public int Step { get; set; }

    [BindProperty]
    public string? Side { get; set; }

    public DrillResult? Last { get; private set; }

    public int? LastGuidedStep { get; private set; }

    public bool GuidedCorrect { get; private set; }

    public bool ShowGuided => LastGuidedStep is not null ||
        (TrainingGuide.SelectedLevel(Company, SandboxId, Request.Cookies[TrainingGuide.CookieName])?.Number == 2 &&
         !Company.IsLevelComplete(2) && Company.Drill.Correct < 5 &&
         Company.Drill.GuidedStepsCompleted < GuidedDrillCatalog.All.Count);

    public string? LastDebit { get; private set; }

    public string? LastCredit { get; private set; }

    public void OnGet()
    {
        if (TempData["GuidedStep"] is int step && step >= 0 && step < GuidedDrillCatalog.All.Count &&
            TempData["GuidedCorrect"] is bool correct)
        {
            LastGuidedStep = step;
            GuidedCorrect = correct;
        }

        if (TempData["DrillLast"] is int id && TempData["DrillOk"] is bool ok)
        {
            Last = new DrillResult(DrillCatalog.Get(id), ok, ok, ok);
            LastDebit = TempData["DrillDr"] as string;
            LastCredit = TempData["DrillCr"] as string;
        }
    }

    public IActionResult OnPostGuided()
    {
        if (!ShowGuided)
        {
            return Redirect("/Training/Drill");
        }

        if (Side is not ("DEBIT" or "CREDIT"))
        {
            ShowError("CHOOSE DEBIT OR CREDIT");
            return Page();
        }

        if (!TryUpdate(c => c.AnswerGuidedDrill(Step, Side), out var correct))
        {
            return Page();
        }

        TempData["GuidedStep"] = Step;
        TempData["GuidedCorrect"] = correct;
        return Redirect("/Training/Drill");
    }

    public IActionResult OnPost()
    {
        var dr = Debit?.Trim() ?? "";
        var cr = Credit?.Trim() ?? "";
        if (dr.Length == 0 || cr.Length == 0)
        {
            ShowError("ENTER BOTH A DEBIT AND A CREDIT ACCOUNT CODE");
            return Page();
        }

        if (TryUpdate(c => c.AnswerDrill(dr, cr), out var result))
        {
            TempData["DrillLast"] = result.Question.Id;
            TempData["DrillOk"] = result.IsCorrect;
            TempData["DrillDr"] = dr.Length > 4 ? dr[..4] : dr;
            TempData["DrillCr"] = cr.Length > 4 ? cr[..4] : cr;
        }

        return Redirect("/Training/Drill");
    }
}
