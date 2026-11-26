using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages.Gl;

public class NewAccountModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public string? Code { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public AccountType Type { get; set; } = AccountType.Expense;

    [BindProperty]
    public string? Description { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!Enum.IsDefined(Type))
        {
            ShowError("SELECT A VALID ACCOUNT TYPE");
            return Page();
        }

        if (!TryUpdate(c => c.AddAccount(Code, Name, Type, Description), out var account))
        {
            return Page();
        }

        Flash($"GL201I ACCOUNT {account.Code} {account.Name} CREATED AS {account.Type.ToString().ToUpperInvariant()}");
        return Redirect("/Gl/Accounts");
    }
}
