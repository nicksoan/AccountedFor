using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Gl;

public class JournalModel(SandboxStore store) : TerminalPageModel(store)
{
    public const int LineCount = 8;

    public sealed class LineInput
    {
        public string? Account { get; set; }
        public string? Debit { get; set; }
        public string? Credit { get; set; }
        public string? Memo { get; set; }
    }

    [BindProperty]
    public string? Date { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string? Reference { get; set; }

    [BindProperty]
    public List<LineInput> Lines { get; set; } = [];

    public void OnGet()
    {
        Date = Fmt.Date(Today);
        Pad();
    }

    public IActionResult OnPost()
    {
        Pad();
        if (!TryParseDate(Date, out var date))
        {
            return Page();
        }

        var inputs = new List<JournalLineInput>();
        for (var i = 0; i < Lines.Count; i++)
        {
            var l = Lines[i];
            if (string.IsNullOrWhiteSpace(l.Account) && string.IsNullOrWhiteSpace(l.Debit) && string.IsNullOrWhiteSpace(l.Credit))
            {
                continue;
            }

            if (!TryParseAmount(l.Debit, out var dr, $"LINE {i + 1} DEBIT") || !TryParseAmount(l.Credit, out var cr, $"LINE {i + 1} CREDIT"))
            {
                return Page();
            }

            inputs.Add(new JournalLineInput(l.Account, dr, cr, l.Memo));
        }

        if (!TryUpdate(c => c.PostJournal(date, Description, inputs, Reference), out var journal))
        {
            return Page();
        }

        Flash($"GL301I JOURNAL {Fmt.Jnl(journal.Number)} POSTED - DR {Fmt.Amt(journal.TotalDebits)} = CR {Fmt.Amt(journal.TotalCredits)}");
        return Redirect($"/Gl/JournalView/{journal.Number}");
    }

    private void Pad()
    {
        if (Lines.Count > LineCount)
        {
            Lines = Lines.Take(LineCount).ToList();
        }

        while (Lines.Count < LineCount)
        {
            Lines.Add(new LineInput());
        }
    }
}
