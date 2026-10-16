// Web.CookiePolicy, Example 1: cookie consent.
//
// When CheckConsentNeeded returns true, the policy drops every non-essential cookie until the
// visitor consents. The cart cookie is marked essential, so it is always issued. The tracking
// cookie is issued only after /consent has set the consent cookie.
//
//   dotnet run
//   curl -i -c jar.txt -b jar.txt http://localhost:5000/          (cart cookie only)
//   curl -i -c jar.txt -b jar.txt http://localhost:5000/consent   (the consent cookie)
//   curl -i -c jar.txt -b jar.txt http://localhost:5000/          (cart and tracking cookies)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.CookiePolicy;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

// Ahead of everything that writes a cookie.
application.UseCookiePolicy(options => options.CheckConsentNeeded = _ => true);
application.UseRouting();

application.MapGet("/", (IHttpContext context) =>
{
    context.Response.Cookies.Add(new HttpCookie("cart", "3-items", new HttpCookieOptions { IsEssential = true }));
    context.Response.Cookies.Add(new HttpCookie("tracking", "visitor-42"));

    ICookieConsentFeature consent = context.Features.Get<ICookieConsentFeature>()!;
    return consent.CanTrack
        ? "Cart and tracking cookies set."
        : "Cart cookie set. The tracking cookie waits for your consent: GET /consent.";
});

application.MapGet("/consent", (IHttpContext context) =>
{
    context.Features.Get<ICookieConsentFeature>()!.GrantConsent();
    return "Consent recorded.";
});

await application.RunAsync();
