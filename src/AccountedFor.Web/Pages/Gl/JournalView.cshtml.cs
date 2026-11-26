using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Gl;

public class JournalViewModel(SandboxStore store) : TerminalPageModel(store)
{
    public JournalEntry Journal { get; private set; } = null!;

    [BindProperty]
    public string? ReversalDate { get; set; }

    public IActionResult OnGet(int number)
    {
        if (!Load(number))
        {
            return Redirect("/Gl/Journals");
        }

        ReversalDate = Fmt.Date(Journal.Date > Today ? Journal.Date : Today);
        return Page();
    }

    public IActionResult OnPost(int number)
    {
        if (!Load(number))
        {
            return Redirect("/Gl/Journals");
        }

        if (!TryParseDate(ReversalDate, out var date, "REVERSAL DATE")
            || !TryUpdate(c => c.ReverseJournal(number, date), out var reversal))
        {
            return Page();
        }

        Flash($"GL411I JOURNAL {Fmt.Jnl(number)} REVERSED BY {Fmt.Jnl(reversal.Number)}");
        return Redirect($"/Gl/JournalView/{reversal.Number}");
    }

    private bool Load(int number)
    {
        if (Company.FindJournal(number) is { } j)
        {
            Journal = j;
            return true;
        }

        TempData["Error"] = $"JOURNAL {Fmt.Jnl(number)} NOT FOUND";
        return false;
    }
}
