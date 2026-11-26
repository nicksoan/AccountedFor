using AccountedFor.Web.Terminal;
using Fidos.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Pages;

public class IndexModel(SandboxStore store) : PageModel
{
    [BindProperty]
    public string? OperatorId { get; set; }

    [BindProperty]
    public string? SandboxKey { get; set; }

    [BindProperty]
    public string? StartNew { get; set; }

    public Company? Existing { get; private set; }

    public void OnGet()
    {
        Existing = store.Find(Request.Cookies[TerminalPageModel.CookieName]);
        OperatorId = Existing?.OperatorId;
        StartNew = "N";
    }

    public IActionResult OnPost()
    {
        var cookieId = Request.Cookies[TerminalPageModel.CookieName];
        Existing = store.Find(cookieId);

        var op = OperatorId?.Trim().ToUpperInvariant() ?? "";
        if (op.Length is 0 or > 8 || !op.All(char.IsAsciiLetterOrDigit))
        {
            ViewData["Error"] = "USERID MUST BE 1-8 LETTERS OR DIGITS";
            return Page();
        }

        string id;
        if (!string.IsNullOrWhiteSpace(SandboxKey))
        {
            var key = SandboxKey.Trim().Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
            if (store.Find(key) is null)
            {
                ViewData["Error"] = "SANDBOX ID NOT FOUND - CHECK IT, OR LEAVE IT BLANK TO START A NEW ONE";
                return Page();
            }

            id = key;
        }
        else if (Existing is not null && !string.Equals(StartNew?.Trim(), "Y", StringComparison.OrdinalIgnoreCase))
        {
            id = cookieId!.ToLowerInvariant();
        }
        else
        {
            id = store.Create(op);
        }

        var company = store.Find(id)!;
        store.Update(id, company, c => c.OperatorId = op, out _);

        Response.Cookies.Append(TerminalPageModel.CookieName, id, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
        });
        TempData["Message"] = $"FID001I LOGON ACCEPTED - WELCOME TO NORTHSEA, {op}";
        return Redirect(Navigation.MainMenu);
    }
}
