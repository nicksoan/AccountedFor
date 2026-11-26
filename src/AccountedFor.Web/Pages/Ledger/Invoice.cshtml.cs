using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Ledger;

public class InvoiceModel(SandboxStore store) : LedgerPageModel(store)
{
    public const int LineCount = 5;

    public sealed class LineInput
    {
        public string? Account { get; set; }
        public string? Description { get; set; }
        public string? Net { get; set; }
    }

    [BindProperty(SupportsGet = true)]
    public string? Party { get; set; }

    [BindProperty]
    public string? Date { get; set; }

    [BindProperty]
    public string? Reference { get; set; }

    [BindProperty]
    public string? VatRate { get; set; } = "20";

    [BindProperty]
    public List<LineInput> Lines { get; set; } = [];

    public void OnGet()
    {
        Date = Fmt.Date(Today);
        Pad();
        Lines[0].Account = Kind.DefaultLineAccount;
    }

    public IActionResult OnPost()
    {
        Pad();
        if (!TryParseDate(Date, out var date, "INVOICE DATE") || !TryParseAmount(VatRate, out var vat, "VAT RATE"))
        {
            return Page();
        }

        var inputs = new List<DocumentLineInput>();
        for (var i = 0; i < Lines.Count; i++)
        {
            var l = Lines[i];
            if (string.IsNullOrWhiteSpace(l.Net) && string.IsNullOrWhiteSpace(l.Description))
            {
                continue;
            }

            if (!TryParseAmount(l.Net, out var net, $"LINE {i + 1} NET AMOUNT"))
            {
                return Page();
            }

            inputs.Add(new DocumentLineInput(l.Account, l.Description, net));
        }

        if (!TryUpdate(c => Kind.PostInvoice(c, Party, date, inputs, vat, Reference), out var invoice))
        {
            return Page();
        }

        Flash($"{Kind.Code}301I {Kind.InvoiceTitle} {invoice.Number} POSTED: NET {Fmt.Amt(invoice.Net)} + VAT {Fmt.Amt(invoice.Vat)} = {Fmt.Amt(invoice.Gross)} - GL JOURNAL {Fmt.Jnl(invoice.JournalNumber)}");
        return Redirect($"/Gl/JournalView/{invoice.JournalNumber}");
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
