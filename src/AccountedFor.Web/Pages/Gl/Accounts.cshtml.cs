using AccountedFor.Web.Terminal;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Gl;

public class AccountsModel(SandboxStore store) : TerminalPageModel(store)
{
    public void OnGet() => Milestone(Milestones.ChartOfAccounts);
}
