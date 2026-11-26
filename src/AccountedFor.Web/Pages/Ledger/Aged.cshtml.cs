using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Ledger;

public class AgedModel(SandboxStore store) : LedgerPageModel(store)
{
    public string? AsOf { get; private set; }

    public IReadOnlyList<AgedRow> Rows { get; private set; } = [];

    public void OnGet(string? asOf)
    {
        AsOf = string.IsNullOrWhiteSpace(asOf) ? Fmt.Date(Today) : asOf.Trim();
        if (TryParseDate(AsOf, out var date, "AS AT DATE"))
        {
            Rows = Kind.Aged(Company, date);
            if (Kind.IsReceivables)
            {
                Milestone(Milestones.AgedDebtors);
            }
        }
    }
}
