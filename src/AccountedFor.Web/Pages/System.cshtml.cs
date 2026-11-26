using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages;

public class SystemModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public string? Confirm { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!string.Equals(Confirm?.Trim(), "RESET", StringComparison.OrdinalIgnoreCase))
        {
            ShowError("RESET NOT CONFIRMED - TYPE THE WORD RESET");
            return Page();
        }

        Store.Reset(SandboxId, Company.OperatorId);
        Flash("FID900I SANDBOX RESET - YOU HAVE A FRESH SET OF BOOKS");
        return Redirect(Navigation.MainMenu);
    }
}
