using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages;

public class OptionsModel(SandboxStore store) : TerminalPageModel(store)
{
    [BindProperty]
    public bool DisableCrt { get; set; }

    [BindProperty]
    public string FontColor { get; set; } = "green";

    public void OnGet()
    {
        DisableCrt = Request.Cookies[DisplayPreferences.DisableCrtCookie] == "1";
        FontColor = DisplayPreferences.FontColorOrDefault(Request.Cookies[DisplayPreferences.FontColorCookie]);
    }

    public IActionResult OnPost()
    {
        if (!DisplayPreferences.IsSupportedFontColor(FontColor))
        {
            ShowError("SELECT A VALID FONT COLOUR");
            return Page();
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
        };
        Response.Cookies.Append(DisplayPreferences.DisableCrtCookie, DisableCrt ? "1" : "0", cookieOptions);
        Response.Cookies.Append(DisplayPreferences.FontColorCookie, FontColor, cookieOptions);
        Flash("DISPLAY OPTIONS SAVED");
        return RedirectToPage();
    }
}
