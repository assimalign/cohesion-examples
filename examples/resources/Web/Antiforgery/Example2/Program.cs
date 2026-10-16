// Web.Antiforgery, Example 2: a server-rendered form.
//
// GET / renders a comment form that carries the request token in a hidden field. Every route in
// the /comments group requires antiforgery, so a form post must include that field. The webhook
// route opts out because its caller is a server, not a browser. The keys that protect tokens live
// in a key ring on disk, so tokens issued before a restart stay valid after it.
//
//   dotnet run
//   Open http://localhost:5000 and post a comment, or with curl:
//   TOKEN=$(curl -s -c cookies.txt http://localhost:5000/ | grep -o 'value="[^"]*"' | head -1 | cut -d'"' -f2)
//   curl -i -b cookies.txt --data-urlencode "__RequestVerificationToken=$TOKEN" -d "text=hi" http://localhost:5000/comments
//   curl -i -X POST http://localhost:5000/comments/webhook        (no token needed)

using System;
using System.IO;
using System.Text;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Security.DataProtection;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Antiforgery;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

DataProtectionProvider dataProtection = DataProtectionProvider.Create(
    FileSystemKeyRepository.Create(Path.Combine(AppContext.BaseDirectory, "keys")));

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAntiforgery(antiforgery =>
    {
        antiforgery.UseDataProtection(dataProtection);
        antiforgery.Options.CookieName = "example-antiforgery";
    });

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseAntiforgery();

application.MapGet("/", async (IHttpContext context) =>
{
    HttpAntiforgeryTokenSet tokens = context.RequireAntiforgery.GetAndStoreTokens(context);

    string html = $"""
        <!doctype html>
        <form method="post" action="/comments">
          <input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">
          <input name="text" placeholder="Your comment">
          <button>Post</button>
        </form>
        """;

    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/html; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(html), context.RequestCancelled);
});

// One verb protects every route in the group.
IRouterGroupBuilder comments = application.MapGroup("/comments").RequireAntiforgery();

// UseAntiforgery has already read the form to find the token, so the handler reads it freely.
comments.MapPost("", (IHttpContext context) => $"Comment received: {context.Request.Form["text"].Value}");

comments.MapPost("webhook", () => "Webhook accepted.")
    .DisableAntiforgery();

await application.RunAsync();
