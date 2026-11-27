using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages;

public class SystemModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public string? Confirm { get; set; }

    [BindProperty]
    public string? ResumeId { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPostReset()
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

    public IActionResult OnPostResume()
    {
        var key = ResumeId?.Trim().Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
        if (key is null || Store.Find(key) is null)
        {
            ShowError("SANDBOX ID NOT FOUND - CHECK THE ID AND TRY AGAIN");
            return Page();
        }

        SetSandboxCookie(HttpContext, CookieName, key);
        Flash("FID900I SANDBOX OPENED");
        return Redirect(Navigation.MainMenu);
    }

    public IActionResult OnPostStartNew()
    {
        SetSandboxCookie(HttpContext, CookieName, Store.Create(Company.OperatorId));
        Flash("FID900I NEW SANDBOX CREATED");
        return Redirect(Navigation.MainMenu);
    }
}
