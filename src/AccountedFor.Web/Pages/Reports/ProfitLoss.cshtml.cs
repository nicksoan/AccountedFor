using AccountedFor.Web.Terminal;
using Fidos.Core;
using Fidos.Core.Training;

namespace AccountedFor.Web.Pages.Reports;

public class ProfitLossModel(SandboxStore store) : TerminalPageModel(store)
{
    public string? From { get; private set; }

    public string? To { get; private set; }

    public IncomeStatement? Statement { get; private set; }

    public void OnGet(string? from, string? to)
    {
        From = string.IsNullOrWhiteSpace(from) ? Fmt.Date(new DateOnly(Today.Year, 1, 1)) : from.Trim();
        To = string.IsNullOrWhiteSpace(to) ? Fmt.Date(new DateOnly(Today.Year, 12, 31)) : to.Trim();
        if (TryParseDate(From, out var f, "FROM DATE") && TryParseDate(To, out var t, "TO DATE"))
        {
            if (f > t)
            {
                ShowError("FROM DATE MUST NOT BE AFTER TO DATE");
                return;
            }

            Statement = Company.ProfitAndLoss(f, t);
            Milestone(Milestones.ProfitAndLoss);
        }
    }
}
