using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages;

public class LogoffModel(SandboxStore store) : TerminalPageModel(store)
{
    public bool LoggedOff { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        SetSandboxCookie(HttpContext, RememberedCookieName, SandboxId);
        Response.Cookies.Delete(CookieName);
        LoggedOff = true;
        ViewData["Company"] = null;
        return Page();
    }
}
