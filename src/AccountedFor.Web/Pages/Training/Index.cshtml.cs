using AccountedFor.Web.Terminal;

namespace AccountedFor.Web.Pages.Training;

public class IndexModel(SandboxStore store) : TerminalPageModel(store)
{
    public void OnGet() => CatchUpLevels();
}
