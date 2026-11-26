using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Ledger;

public class NewPartyModel(SandboxStore store) : LedgerPageModel(store)
{
    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public string? Contact { get; set; }

    [BindProperty]
    public string? Terms { get; set; } = "30";

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!int.TryParse(Terms?.Trim(), out var terms))
        {
            ShowError("PAYMENT TERMS MUST BE A WHOLE NUMBER OF DAYS");
            return Page();
        }

        if (!TryUpdate(c => Kind.AddParty(c, Name, Contact, terms), out var id))
        {
            return Page();
        }

        Flash($"{Kind.Code}201I {Kind.Party} {id} CREATED");
        return Redirect(Kind.Url("parties/" + id));
    }
}
