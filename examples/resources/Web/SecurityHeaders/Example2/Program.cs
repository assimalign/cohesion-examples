// Web.SecurityHeaders, Example 2: a Content Security Policy with nonces.
//
// The policy allows scripts only when they carry the nonce generated for this response, so a
// script an attacker injects into the page does not run. Plugins (object-src) and <base> are
// blocked, the page gets its own browsing-context group (Cross-Origin-Opener-Policy), and the
// camera and microphone are turned off. One route changes the policy: /embed may be framed by
// https://partner.example, while every other page stays unframeable.
//
//   dotnet run
//   Open http://localhost:5000: the nonce script runs, the injected one is blocked (see the console).
//   curl -i http://localhost:5000/          (Content-Security-Policy with a fresh nonce on every request)
//   curl -i http://localhost:5000/embed     (frame-ancestors https://partner.example)

using System.Text;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.SecurityHeaders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseSecurityHeaders(policy =>
{
    policy.ContentSecurityPolicy = ContentSecurityPolicy.Create(csp => csp
        .DefaultSrc(sources => sources.Self())
        .ScriptSrc(sources => sources.Nonce())
        .ObjectSrc(sources => sources.None())
        .BaseUri(sources => sources.None()));
    policy.CrossOriginOpenerPolicy = CrossOriginOpenerPolicy.SameOrigin;
    policy.PermissionsPolicy = PermissionsPolicy.Create(permissions => permissions
        .Disable("camera")
        .Disable("microphone"));
});
application.UseRouting();

application.MapGet("/", async (IHttpContext context) =>
{
    // The nonce the Content-Security-Policy header of this response allows.
    string nonce = context.Features.Get<ISecurityHeadersFeature>()!.Nonce;

    string html = $"""
        <!doctype html>
        <h1 id="status">Waiting for scripts...</h1>
        <script nonce="{nonce}">document.getElementById("status").textContent = "The nonce script ran.";</script>
        <script>document.body.append("The injected script ran. It should not have!");</script>
        """;

    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/html; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(html), context.RequestCancelled);
});

application.MapGet("/embed", () => "A widget partners may embed.")
    .WithSecurityHeaders(policy => policy.Framing = FramingPolicy.AllowFrom("https://partner.example"));

await application.RunAsync();
