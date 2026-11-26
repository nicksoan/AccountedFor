using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Reports;

public class TrialBalanceModel(SandboxStore store) : TerminalPageModel(store)
{
    public string? AsOf { get; private set; }

    public IReadOnlyList<TrialBalanceRow> Rows { get; private set; } = [];

    public void OnGet(string? asOf)
    {
        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(asOf))
        {
            AsOf = asOf.Trim();
            if (TryParseDate(AsOf, out var d, "AS AT DATE"))
            {
                date = d;
            }
        }

        Rows = Company.TrialBalance(date);
        Milestone(Milestones.TrialBalance);
    }
}
