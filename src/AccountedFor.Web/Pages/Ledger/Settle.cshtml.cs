using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Ledger;

public class SettleModel(SandboxStore store) : LedgerPageModel(store)
{
    [BindProperty(SupportsGet = true)]
    public string? Party { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Invoice { get; set; }

    [BindProperty]
    public string? Date { get; set; }

    [BindProperty]
    public string? Amount { get; set; }

    [BindProperty]
    public string? Bank { get; set; } = SystemAccounts.Bank;

    [BindProperty]
    public string? Reference { get; set; }

    public void OnGet()
    {
        Date = Fmt.Date(Today);
        var invoice = Kind.Invoices(Company).FirstOrDefault(i => string.Equals(i.Number, Invoice, StringComparison.OrdinalIgnoreCase));
        if (invoice is not null)
        {
            Party = invoice.PartyId;
            Invoice = invoice.Number;
            Amount = invoice.Outstanding.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (Kind.FindParty(Company, Party) is { } party && party.Balance > 0)
        {
            Invoice = null;
            Amount = party.Balance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            Invoice = null;
        }
    }

    public IActionResult OnPost()
    {
        if (!TryParseDate(Date, out var date) || !TryParseAmount(Amount, out var amount, "AMOUNT"))
        {
            return Page();
        }

        var invoice = string.IsNullOrWhiteSpace(Invoice) ? null : Invoice;
        if (!TryUpdate(c => Kind.PostSettlement(c, Party, date, amount, Bank, invoice, Reference), out var s))
        {
            return Page();
        }

        var allocations = string.Join(", ", s.Allocations.Select(a => $"{a.InvoiceNumber} {Fmt.Amt(a.Amount)}"));
        Flash($"{Kind.Code}401I {s.Number} POSTED {Fmt.Amt(s.Amount)} ALLOCATED TO {allocations} - GL JOURNAL {Fmt.Jnl(s.JournalNumber)}");
        return Redirect($"/Gl/JournalView/{s.JournalNumber}");
    }
}
