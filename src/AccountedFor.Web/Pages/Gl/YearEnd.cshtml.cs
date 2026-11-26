using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Gl;

public class YearEndModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public string? Date { get; set; }

    public IReadOnlyList<(Account Account, decimal Balance)>? Preview { get; private set; }

    public void OnGet(string? date)
    {
        Date = string.IsNullOrWhiteSpace(date) ? Fmt.Date(new DateOnly(Today.Year, 12, 31)) : date.Trim();
        if (date is not null && TryParseDate(Date, out var d, "YEAR-END DATE"))
        {
            Preview = Company.Accounts
                .Where(a => a.Type is AccountType.Revenue or AccountType.Expense)
                .Select(a => (a, Company.Balance(a.Code, d)))
                .Where(x => x.Item2 != 0)
                .ToList();
        }
    }

    public IActionResult OnPost()
    {
        if (!TryParseDate(Date, out var date, "YEAR-END DATE") || !TryUpdate(c => c.CloseYear(date), out var journal))
        {
            return Page();
        }

        Flash($"GL601I YEAR CLOSED AT {Fmt.Date(date)} - JOURNAL {Fmt.Jnl(journal.Number)} POSTED TO RETAINED EARNINGS");
        return Redirect($"/Gl/JournalView/{journal.Number}");
    }
}
