using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Pages;

public class IndexModel(SandboxStore store) : PageModel
{
    [BindProperty]
    public string? OperatorId { get; set; }

    public Company? Existing { get; private set; }

    public void OnGet()
    {
        FindExisting();
        OperatorId = Existing?.OperatorId;
    }

    public IActionResult OnPost()
    {
        var existingId = FindExisting();

        var op = OperatorId?.Trim().ToUpperInvariant() ?? "";
        if (op.Length is 0 or > 8 || !op.All(char.IsAsciiLetterOrDigit))
        {
            ViewData["Error"] = "USERID MUST BE 1-8 LETTERS OR DIGITS";
            return Page();
        }

        var id = existingId ?? store.Create(op);
        if (Existing is not null && Existing.OperatorId != op)
        {
            store.Update(id, Existing, c => c.OperatorId = op, out _);
        }

        TerminalPageModel.SetSandboxCookie(HttpContext, TerminalPageModel.CookieName, id);
        if (Request.Cookies.ContainsKey(TerminalPageModel.RememberedCookieName))
        {
            Response.Cookies.Delete(TerminalPageModel.RememberedCookieName);
        }
        TempData["Message"] = $"FID001I LOGON ACCEPTED - WELCOME TO NORTHSEA, {op}";
        TempData["ShowCommandStep"] = true;
        return Redirect(Navigation.MainMenu);
    }

    private string? FindExisting()
    {
        var id = Request.Cookies[TerminalPageModel.CookieName];
        Existing = store.Find(id);
        if (Existing is null)
        {
            id = Request.Cookies[TerminalPageModel.RememberedCookieName];
            Existing = store.Find(id);
        }

        return Existing is null ? null : id;
    }
}
