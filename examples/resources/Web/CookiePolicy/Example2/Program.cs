// Web.CookiePolicy, Example 2: enforce cookie attributes.
//
// The policy judges every cookie the application appends. Each one becomes HttpOnly, and a
// SameSite=None cookie without Secure is rejected instead of being upgraded to Secure. Consent is
// required only when the X-Region header says EU, so there the non-essential cookies are dropped
// too. Every rejection is logged to the console.
//
//   dotnet run
//   curl -i http://localhost:5000/                     (session and preferences, both HttpOnly)
//   curl -i -H "X-Region: EU" http://localhost:5000/   (session only)

using System;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.CookiePolicy;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseCookiePolicy(options =>
{
    options.HttpOnly = CookieHttpOnlyPolicy.Always;
    options.SameSiteNoneWithoutSecure = CookieViolationAction.Reject;
    options.CheckConsentNeeded = context => context.Request.Headers["X-Region"].Value == "EU";
    options.OnRejected = rejection => Console.WriteLine($"Rejected cookie '{rejection.Cookie.Name}': {rejection.Reason}");
});
application.UseRouting();

application.MapGet("/", (IHttpContext context) =>
{
    context.Response.Cookies.Add(new HttpCookie("session", "abc123", new HttpCookieOptions { IsEssential = true }));
    context.Response.Cookies.Add(new HttpCookie("preferences", "dark-mode"));
    context.Response.Cookies.Add(new HttpCookie("widget", "embedded", new HttpCookieOptions { SameSite = HttpCookieSameSiteMode.None }));
    return "Three cookies appended. Compare them with the Set-Cookie headers.";
});

await application.RunAsync();
