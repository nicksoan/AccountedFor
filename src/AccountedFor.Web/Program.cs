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
    var isPage = context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>() is not null;
    var gate = isPage
        ? context.RequestServices.GetRequiredService<SandboxStore>().GateFor(context.Request.Cookies[TerminalPageModel.CookieName])
        : null;
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
