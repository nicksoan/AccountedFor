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

    public DrillResult? Last { get; private set; }

    public string? LastDebit { get; private set; }

    public string? LastCredit { get; private set; }

    public void OnGet()
    {
        if (TempData["DrillLast"] is int id && TempData["DrillOk"] is bool ok)
        {
            Last = new DrillResult(DrillCatalog.Get(id), ok, ok, ok);
            LastDebit = TempData["DrillDr"] as string;
            LastCredit = TempData["DrillCr"] as string;
        }
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
