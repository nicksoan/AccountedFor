using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Gl;

public class InquiryModel(SandboxStore store) : TerminalPageModel(store)
{
    public string? Code { get; private set; }

    public Account? Account { get; private set; }

    public IReadOnlyList<LedgerActivityRow> Rows { get; private set; } = [];

    public void OnGet(string? code)
    {
        Code = code?.Trim();
        if (string.IsNullOrEmpty(Code))
        {
            return;
        }

        Account = Company.FindAccount(Code);
        if (Account is null)
        {
            ShowError($"ACCOUNT {Code.ToUpperInvariant()} NOT FOUND");
            return;
        }

        Rows = Company.AccountActivity(Account.Code);
        Milestone(Milestones.AccountInquiry);
    }
}
