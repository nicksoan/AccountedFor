using AccountedFor.Web.Pages;
using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fidos.Core.Tests;

public class SignOnTests : IDisposable
{
    private readonly string _dataPath = Directory.CreateTempSubdirectory().FullName;

    [Fact]
    public void NewVisitor_CreatesSandboxAndGetsCommandStep()
    {
        var store = CreateStore();
        var page = CreatePage(store);
        page.OnGet();
        Assert.Null(page.OperatorId);

        page.OperatorId = " Alex1 ";
        Assert.Equal(Navigation.MainMenu, Assert.IsType<RedirectResult>(page.OnPost()).Url);
        var sandboxFile = Assert.Single(Directory.GetFiles(_dataPath));
        var id = Path.GetFileNameWithoutExtension(sandboxFile);
        Assert.Equal("ALEX1", store.Find(id)?.OperatorId);
        Assert.Contains($"{TerminalPageModel.CookieName}={id}", page.Response.Headers.SetCookie.ToString());
        Assert.Equal(true, page.TempData["ShowCommandStep"]);
    }

    [Theory]
    [InlineData(TerminalPageModel.CookieName)]
    [InlineData(TerminalPageModel.RememberedCookieName)]
    public void ReturningVisitor_SeesSavedUserIdAndReopensSameSandbox(string cookieName)
    {
        var store = CreateStore();
        var id = store.Create("ALEX1");
        var page = CreatePage(store, $"{cookieName}={id}");

        page.OnGet();
        Assert.Equal("ALEX1", page.OperatorId);
        Assert.Same(store.Find(id), page.Existing);
        Assert.IsType<RedirectResult>(page.OnPost());
        Assert.Single(Directory.GetFiles(_dataPath));
        Assert.Contains($"{TerminalPageModel.CookieName}={id}", page.Response.Headers.SetCookie.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("two words")]
    [InlineData("abcdefghi")]
    public void InvalidUserId_DoesNotCreateSandbox(string userId)
    {
        var page = CreatePage(CreateStore());
        page.OperatorId = userId;

        Assert.IsType<PageResult>(page.OnPost());
        Assert.NotNull(page.ViewData["Error"]);
        Assert.Empty(Directory.GetFiles(_dataPath));
        Assert.False(page.Response.Headers.ContainsKey("Set-Cookie"));
    }

    public void Dispose() => Directory.Delete(_dataPath, recursive: true);

    private SandboxStore CreateStore() => new(
        Options.Create(new SandboxOptions { DataPath = _dataPath }),
        WebApplication.CreateBuilder().Environment,
        NullLogger<SandboxStore>.Instance);

    private static IndexModel CreatePage(SandboxStore store, string cookie = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = cookie;
        return new IndexModel(store)
        {
            PageContext = new PageContext
            {
                HttpContext = context,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(context, new EmptyTempDataProvider()),
        };
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
