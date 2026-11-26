using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Reports;

public class ReconcileModel(SandboxStore store) : TerminalPageModel(store)
{
    public IReadOnlyList<ReconciliationRow> Rows { get; private set; } = [];

    public void OnGet()
    {
        Rows = Company.Reconcile();
        Milestone(Milestones.Reconciliation);
    }
}
