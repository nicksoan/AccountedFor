using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace AccountedFor.Web.Pages;

public class MenuModel(SandboxStore store) : TerminalPageModel(store)
{
    public MenuDefinition Menu { get; private set; } = null!;

    public IActionResult OnGet(string? id)
    {
        var menu = Navigation.FindMenu(id ?? "MAIN");
        if (menu is null)
        {
            return NotFound();
        }

        Menu = menu;
        if (menu.Id == "MAIN")
        {
            CatchUpLevels();
        }

        return Page();
    }
}
