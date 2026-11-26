using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Pages;

/// <summary>Handles the COMMAND ===> line present on every screen.</summary>
public class CommandModel : PageModel
{
    public IActionResult OnGet(string? cmd, string? from)
    {
        var back = !string.IsNullOrEmpty(from) && Url.IsLocalUrl(from) ? from : Navigation.MainMenu;
        if (string.IsNullOrWhiteSpace(cmd))
        {
            return LocalRedirect(back);
        }

        var target = Navigation.Resolve(cmd, back);
        if (target is null)
        {
            TempData["Error"] = $"COMMAND '{cmd.Trim().ToUpperInvariant()}' NOT RECOGNISED - ENTER A MENU OPTION OR TRANSACTION CODE (F1=HELP)";
            return LocalRedirect(back);
        }

        return LocalRedirect(target);
    }
}
