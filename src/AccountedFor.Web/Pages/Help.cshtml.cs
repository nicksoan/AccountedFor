using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountedFor.Web.Pages;

/// <summary>Help is available before logon too, so it does not require a sandbox.</summary>
public class HelpModel(SandboxStore store) : PageModel
{
    public HelpTopic Topic { get; private set; } = HelpTopics.All[0];

    public string? ReturnUrl { get; private set; }

    public void OnGet(string? topic)
    {
        Topic = HelpTopics.Find(topic) ?? HelpTopics.Find("BASICS")!;
        if (store.Find(Request.Cookies[TerminalPageModel.CookieName]) is { } company)
        {
            ViewData["Company"] = company;
        }

        var referer = Request.Headers.Referer.ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri) && uri.Host == Request.Host.Host && Url.IsLocalUrl(uri.PathAndQuery)
            && !uri.AbsolutePath.StartsWith("/Help", StringComparison.OrdinalIgnoreCase))
        {
            ReturnUrl = uri.PathAndQuery;
        }
    }
}
