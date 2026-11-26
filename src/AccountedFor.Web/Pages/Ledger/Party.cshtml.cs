using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Ledger;

public class PartyModel(SandboxStore store) : LedgerPageModel(store)
{
    public PartyInfo Party { get; private set; } = null!;

    public IReadOnlyList<PartyStatementRow> Rows { get; private set; } = [];

    public IActionResult OnGet(string id)
    {
        var party = Kind.FindParty(Company, id);
        if (party is null)
        {
            TempData["Error"] = $"{Kind.Party} {id.ToUpperInvariant()} NOT FOUND";
            return Redirect(Kind.Url("parties"));
        }

        Party = party;
        Rows = Kind.Statement(Company, party.Id);
        return Page();
    }
}
