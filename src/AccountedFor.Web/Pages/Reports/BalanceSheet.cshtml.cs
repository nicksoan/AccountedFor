using AccountedFor.Web.Terminal;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Reports;

public class BalanceSheetModel(SandboxStore store) : TerminalPageModel(store)
{
    public string? AsOf { get; private set; }

    public Fidos.Core.BalanceSheet? Sheet { get; private set; }

    public void OnGet(string? asOf)
    {
        AsOf = string.IsNullOrWhiteSpace(asOf) ? Fmt.Date(new DateOnly(Today.Year, 12, 31)) : asOf.Trim();
        if (TryParseDate(AsOf, out var d, "AS AT DATE"))
        {
            Sheet = Company.BalanceSheet(d);
            Milestone(Milestones.BalanceSheet);
        }
    }
}
