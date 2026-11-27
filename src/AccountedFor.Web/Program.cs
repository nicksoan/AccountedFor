using AccountedFor.Web.Terminal;
using Microsoft.AspNetCore.Mvc.RazorPages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection("Sandbox"));
builder.Services.AddSingleton<SandboxStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// One request at a time per sandbox: page handlers and views share the sandbox's in-memory books.
app.Use(async (context, next) =>
{
    var page = context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>();
    var store = page is null ? null : context.RequestServices.GetRequiredService<SandboxStore>();
    var gate = store?.GateFor(context.Request.Cookies[TerminalPageModel.CookieName]);
    if (gate is null && page?.ViewEnginePath == "/Index")
    {
        gate = store?.GateFor(context.Request.Cookies[TerminalPageModel.RememberedCookieName]);
    }
    if (gate is null)
    {
        await next(context);
        return;
    }

    await gate.WaitAsync(context.RequestAborted);
    try
    {
        await next(context);
    }
    finally
    {
        gate.Release();
    }
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
