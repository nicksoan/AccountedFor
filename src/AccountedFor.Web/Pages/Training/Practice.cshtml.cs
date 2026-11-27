using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Pages.Training;

public class PracticeModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public int? MissionIndex { get; set; }

    [BindProperty]
    public string? DebitCode { get; set; }

    [BindProperty]
    public string? DebitAmount { get; set; }

    [BindProperty]
    public string? CreditCode { get; set; }

    [BindProperty]
    public string? CreditAmount { get; set; }

    public Company Books { get; private set; } = null!;

    public IActionResult OnGet()
    {
        CatchUpLevels();
        return Company.IsLevelUnlocked(2) ? OpenBooks() : Redirect("/Training/Level/2");
    }

    public IActionResult OnPost()
    {
        if (!Company.IsLevelUnlocked(2))
        {
            return Redirect("/Training/Level/2");
        }

        if (MissionIndex is not int index || index != Company.PracticeMissionsCompleted || index >= PracticeCatalog.All.Count)
        {
            Flash("The practice books have moved on. Your latest progress is shown below.");
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(DebitAmount) || string.IsNullOrWhiteSpace(CreditAmount))
        {
            ShowError("Enter an amount on both sides before posting.");
            return OpenBooks();
        }

        if (!TryParseAmount(DebitAmount, out var debit, "Debit amount") ||
            !TryParseAmount(CreditAmount, out var credit, "Credit amount") ||
            !TryUpdate(c => c.PostPracticeEntry(index, DebitCode, debit, CreditCode, credit), out var entry))
        {
            return OpenBooks();
        }

        Flash($"Practice journal {Fmt.Jnl(entry.Number)} posted: {entry.Description}. Both sides balance at £{Fmt.Amt(entry.TotalDebits)}.");
        return RedirectToPage();
    }

    private PageResult OpenBooks()
    {
        Books = PracticeCatalog.OpenBooks(Company.PracticeMissionsCompleted);
        ViewData["GuideLevel"] = LevelCatalog.Get(2);
        return Page();
    }
}
